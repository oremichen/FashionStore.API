using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Products;

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
                var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
                var expired = await orders.GetExpiredReservationsAsync(DateTimeOffset.UtcNow, stoppingToken);
                foreach (var reservation in expired)
                {
                    reservation.Expire(DateTimeOffset.UtcNow);
                    var product = await products.GetByIdAsync(reservation.ProductId, true, stoppingToken);
                    product?.ReleaseStock(reservation.Quantity);
                }
                if (expired.Count > 0) await orders.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to release expired inventory reservations.");
            }
        }
    }
}
