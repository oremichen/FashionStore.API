using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Constants;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace FashionStore.Infrastructure.Messages.Inventory;

public sealed class ExpiredInventoryReservationProcessorService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiredInventoryReservationProcessorService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var paymentGatewayFactory = scope.ServiceProvider.GetRequiredService<IPaymentGatewayFactory>();

                var expired = await orderRepository.GetExpiredReservationsAsync(DateTimeOffset.UtcNow, stoppingToken);
                if (expired.Count == 0) continue;

                var candidatesByOrder = expired
                    .Where(r => r.Order is not null)
                    .GroupBy(r => r.OrderId, StringComparer.Ordinal)
                    .Select(group => (Order: group.First().Order!, Reservations: group.ToList()))
                    .ToList();

                var ordersAutoRescued = 0;
                var ordersReleased = 0;
                var ordersDeferred = 0;
                var reservationsReleasedCount = 0;

                foreach (var (order, reservations) in candidatesByOrder)
                {
                    if (string.IsNullOrWhiteSpace(order.PaymentReference)) continue;

                    if (string.Equals(order.PaymentStatus, PaymentStatuses.Success, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogInformation(
                            "Expiry worker skipping order {TrackOrderId} ({PaymentReference}): " +
                            "order is already marked paid; reservations should have been consumed.",
                            order.TrackOrderId, order.PaymentReference);
                        continue;
                    }

                    var providerKey = order.PaymentProvider ?? PaymentProviderKeys.Paystack;
                    var verificationOutcome = await VerifyPaymentOutcomeAsync(
                        order, providerKey, paymentGatewayFactory, stoppingToken);

                    if (verificationOutcome == PaymentCheckOutcome.Deferred)
                    {
                        ordersDeferred++;
                        continue;
                    }

                    if (verificationOutcome == PaymentCheckOutcome.Paid)
                    {
                        logger.LogWarning(
                            "Expiry worker detected paid-but-not-marked order {TrackOrderId} ({PaymentReference}, provider: {Provider}). " +
                            "Attempting rescue (mark paid, consume reservations).",
                            order.TrackOrderId, order.PaymentReference, providerKey);

                        var rescueSucceeded = await TryRescuePaidOrderInlineAsync(
                            orderRepository, order, stoppingToken);

                        if (rescueSucceeded)
                        {
                            ordersAutoRescued++;
                            logger.LogInformation(
                                "Expiry worker successfully rescued order {TrackOrderId} ({PaymentReference}). " +
                                "Reservations consumed.",
                                order.TrackOrderId, order.PaymentReference);
                        }
                        else
                        {
                            logger.LogWarning(
                                "Expiry worker rescue attempt for order {TrackOrderId} ({PaymentReference}) did not confirm success. " +
                                "Deferring reservation release until next run.",
                                order.TrackOrderId, order.PaymentReference);
                            ordersDeferred++;
                        }
                        continue;
                    }

                    var releasedThisOrder = 0;
                    foreach (var reservation in reservations)
                    {
                        try
                        {
                            var released = await orderRepository.ExecuteInRetriableTransactionAsync<bool>(async (tx, ct) =>
                            {
                                var didRelease = await orderRepository.ReleaseInventoryReservationAsync(
                                    reservation.Id, InventoryReservationStatuses.Expired, ct);
                                if (didRelease) await tx.CommitAsync(ct);
                                else await tx.RollbackAsync(ct);
                                return didRelease;
                            }, stoppingToken);

                            if (released)
                            {
                                releasedThisOrder++;
                                reservationsReleasedCount++;
                            }
                            else
                            {
                                logger.LogDebug(
                                    "Expiry worker skipped reservation {ReservationId} on order {OrderId}: " +
                                    "it was no longer Reserved at release time.",
                                    reservation.Id, reservation.OrderId);
                            }
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception releaseEx)
                        {
                            logger.LogError(releaseEx,
                                "Expiry worker failed to release reservation {ReservationId} on order {OrderId}. " +
                                "Will retry on next scheduled run.",
                                reservation.Id, reservation.OrderId);
                        }
                    }
                    if (releasedThisOrder > 0) ordersReleased++;
                }

                logger.LogInformation(
                    "Expiry inventory run complete. Examined {ExpiredCandidates} reservations across {OrderCount} orders. " +
                    "Auto-rescued paid bug-affected orders: {Rescued}. " +
                    "Released abandoned orders: {ReleasedOrders} ({ReleasedReservations} reservations). " +
                    "Deferred due to transient payment-provider/rescue errors: {DeferredOrders}.",
                    expired.Count, candidatesByOrder.Count,
                    ordersAutoRescued, ordersReleased, reservationsReleasedCount, ordersDeferred);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to run expired inventory reservation release worker.");
            }
        }
    }

    private enum PaymentCheckOutcome
    {
        NotPaid,
        Paid,
        Deferred
    }

    private async Task<PaymentCheckOutcome> VerifyPaymentOutcomeAsync(
        Order order,
        string providerKey,
        IPaymentGatewayFactory paymentGatewayFactory,
        CancellationToken cancellationToken)
    {
        if (string.Equals(providerKey, PaymentProviderKeys.PayOnDelivery, StringComparison.OrdinalIgnoreCase))
        {
            return PaymentCheckOutcome.NotPaid;
        }

        IPaymentGateway gateway;
        try
        {
            gateway = paymentGatewayFactory.Get(providerKey);
        }
        catch (ArgumentException gatewayEx)
        {
            logger.LogWarning(gatewayEx,
                "Expiry worker could not resolve payment gateway for order {TrackOrderId} ({PaymentReference}, provider: {Provider}). " +
                "Deferring reservation release until next run.",
                order.TrackOrderId, order.PaymentReference, providerKey);
            return PaymentCheckOutcome.Deferred;
        }

        PaymentVerificationResult verification;
        try
        {
            verification = await gateway.VerifyByMerchantReferenceAsync(order.PaymentReference, cancellationToken);
        }
        catch (HttpRequestException httpEx)
        {
            logger.LogWarning(httpEx,
                "Expiry worker could not reach payment provider {Provider} for order {TrackOrderId} ({PaymentReference}). " +
                "Deferring reservation release until next run.",
                providerKey, order.TrackOrderId, order.PaymentReference);
            return PaymentCheckOutcome.Deferred;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception verifyEx)
        {
            logger.LogError(verifyEx,
                "Expiry worker got unexpected error verifying order {TrackOrderId} ({PaymentReference}, provider: {Provider}). " +
                "Deferring reservation release until next run.",
                order.TrackOrderId, order.PaymentReference, providerKey);
            return PaymentCheckOutcome.Deferred;
        }

        if (!verification.IsSuccess)
        {
            return PaymentCheckOutcome.NotPaid;
        }

        var expectedAmount = checked(decimal.ToInt64(order.Total * 100m));
        var detailsMatch =
            string.Equals(verification.MerchantReference, order.PaymentReference, StringComparison.Ordinal) &&
            verification.AmountKobo == expectedAmount &&
            string.Equals(verification.Currency, order.Currency, StringComparison.OrdinalIgnoreCase);

        if (!detailsMatch)
        {
            logger.LogError(
                "Expiry worker verification mismatch for order {TrackOrderId} ({PaymentReference}, provider: {Provider}). " +
                "Expected amount {ExpectedAmount} {Currency}; received {PaidAmount} {PaidCurrency}. " +
                "Releasing reservations.",
                order.TrackOrderId, order.PaymentReference, providerKey,
                expectedAmount, order.Currency, verification.AmountKobo, verification.Currency);
            return PaymentCheckOutcome.NotPaid;
        }

        return PaymentCheckOutcome.Paid;
    }

    private async Task<bool> TryRescuePaidOrderInlineAsync(
        IOrderRepository orderRepository,
        Order order,
        CancellationToken cancellationToken)
    {
        var paidAt = DateTimeOffset.UtcNow;
        try
        {
            var (commitSucceeded, _) = await orderRepository.ExecuteInRetriableTransactionAsync(async (tx, ct) =>
            {
                string? conflictMessage = null;
                order.MarkPaid(paidAt);
                foreach (var reservation in order.InventoryReservations.Where(item =>
                    item.Status == InventoryReservationStatuses.Reserved))
                {
                    var consumed = await orderRepository.ConsumeInventoryReservationAsync(reservation.Id, ct);
                    if (!consumed)
                    {
                        conflictMessage =
                            $"Inventory reservation {reservation.Id} for order {order.Id} could not be consumed. " +
                            "It was likely released or expired concurrently.";
                        break;
                    }
                }

                if (conflictMessage is not null)
                {
                    logger.LogWarning("{Message} Rolling back rescue for order {OrderId}.",
                        conflictMessage, order.Id);
                    await tx.RollbackAsync(ct);
                    return (Success: false, ConflictMessage: conflictMessage);
                }

                await orderRepository.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return (Success: true, ConflictMessage: (string?)null);
            }, cancellationToken);

            return commitSucceeded;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception rescueEx)
        {
            logger.LogError(rescueEx,
                "Expiry worker inline rescue threw for order {TrackOrderId} ({PaymentReference}).",
                order.TrackOrderId, order.PaymentReference);
            return false;
        }
    }
}
