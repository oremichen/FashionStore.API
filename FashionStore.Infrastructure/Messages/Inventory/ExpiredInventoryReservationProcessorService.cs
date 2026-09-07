using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Constants;

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
                var orders = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                var expired = await orders.GetExpiredReservationsAsync(DateTimeOffset.UtcNow, stoppingToken);

                var releasedCount = 0;
                var skippedCount = 0;
                foreach (var reservation in expired)
                {
                    await using var transaction = await orders.BeginTransactionAsync(stoppingToken);
                    try
                    {
                        var released = await orders.ReleaseInventoryReservationAsync(
                            reservation.Id, InventoryReservationStatuses.Expired, stoppingToken);
                        if (released)
                        {
                            releasedCount++;
                        }
                        else
                        {
                            skippedCount++;
                            logger.LogDebug(
                                "Expiry worker skipped reservation {ReservationId} for order {OrderId}: " +
                                "status was no longer 'reserved' when the release ran.",
                                reservation.Id, reservation.OrderId);
                        }
                        await transaction.CommitAsync(stoppingToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(stoppingToken);
                        throw;
                    }
                }

                if (expired.Count > 0)
                {
                    logger.LogInformation(
                        "Expiry inventory run complete. Found {Total} expired reservations; " +
                        "released {Released}, skipped {Skipped} (already consumed/released).",
                        expired.Count, releasedCount, skippedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to release expired inventory reservations.");
            }
        }
    }
}
