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
    private const string VerifyPaystackInterfaceTypeName
        = "FashionStore.API.Features.Payments.VerifyPaystack.IVerifyPaystackService, FashionStore.API";
    private const string SuccessfulStatuses = "00";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var paystackClient = scope.ServiceProvider.GetRequiredService<IPaystackClient>();

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

                    PaystackVerificationResult paystackResult;
                    try
                    {
                        paystackResult = await paystackClient.VerifyAsync(order.PaymentReference, stoppingToken);
                    }
                    catch (HttpRequestException paystackHttpEx)
                    {
                        ordersDeferred++;
                        logger.LogWarning(paystackHttpEx,
                            "Expiry worker could not reach Paystack for order {TrackOrderId} ({PaymentReference}). " +
                            "Deferring reservation release until next run.",
                            order.TrackOrderId, order.PaymentReference);
                        continue;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception paystackEx)
                    {
                        ordersDeferred++;
                        logger.LogError(paystackEx,
                            "Expiry worker got unexpected error from Paystack Verify on order {TrackOrderId} ({PaymentReference}). " +
                            "Deferring reservation release until next run.",
                            order.TrackOrderId, order.PaymentReference);
                        continue;
                    }

                    if (string.Equals(paystackResult.Status, PaymentStatuses.Success, StringComparison.OrdinalIgnoreCase))
                    {
                        logger.LogWarning(
                            "Expiry worker detected paid-but-not-marked order {TrackOrderId} ({PaymentReference}). " +
                            "Attempting rescue through Verify service (mark paid, consume reservations, send emails).",
                            order.TrackOrderId, order.PaymentReference);
                        try
                        {
                            var rescueSucceeded = await TryRescuePaidOrderViaVerifyServiceAsync(
                                scope.ServiceProvider, order.PaymentReference, stoppingToken);
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
                        }
                        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                        {
                            throw;
                        }
                        catch (Exception rescueEx)
                        {
                            ordersDeferred++;
                            logger.LogError(rescueEx,
                                "Expiry worker rescue attempt threw for order {TrackOrderId} ({PaymentReference}). " +
                                "Deferring reservation release until next run.",
                                order.TrackOrderId, order.PaymentReference);
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
                    "Deferred due to transient Paystack/verify errors: {DeferredOrders}.",
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

    private static async Task<bool> TryRescuePaidOrderViaVerifyServiceAsync(
        IServiceProvider serviceProvider, string paymentReference, CancellationToken cancellationToken)
    {
        var verifyServiceType = Type.GetType(VerifyPaystackInterfaceTypeName, throwOnError: true)!;
        var verifyService = serviceProvider.GetRequiredService(verifyServiceType);
        var executeAsyncMethod = verifyServiceType.GetMethod(
            nameof(IVerifyPaystackShim.ExecuteAsync),
            new[] { typeof(string), typeof(string), typeof(CancellationToken) });
        if (executeAsyncMethod is null)
        {
            throw new InvalidOperationException(
                $"Could not locate ExecuteAsync(string, string, CancellationToken) on {verifyServiceType.FullName}.");
        }

        object?[] parameters = new object?[] { paymentReference, null, cancellationToken };
        var task = (Task)executeAsyncMethod.Invoke(verifyService, parameters)!;
        await task.ConfigureAwait(false);

        var resultProperty = task.GetType().GetProperty("Result");
        if (resultProperty is null) return false;
        var responseResult = resultProperty.GetValue(task);
        if (responseResult is null) return false;

        var statusProperty = responseResult.GetType().GetProperty("Status");
        var statusValue = statusProperty?.GetValue(responseResult) as string;
        return string.Equals(statusValue, ResponseCodes.SUCCESS, StringComparison.Ordinal);
    }
}

internal interface IVerifyPaystackShim
{
    Task<object?> ExecuteAsync(string reference, string? userId, CancellationToken cancellationToken);
}
