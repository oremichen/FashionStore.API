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
                foreach (var reservation in expired)
                    await orders.ReleaseInventoryReservationAsync(reservation.Id, InventoryReservationStatuses.Expired, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to release expired inventory reservations.");
            }
        }
    }
}
