using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Entities;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.Json;

namespace FashionStore.Infrastructure.Repository.OrderRepo;

public sealed class OrderRepository : IOrderRepository
{
    private readonly FashionStoreDbContext _dbContext;

    public OrderRepository(FashionStoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IOrderTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfCoreOrderTransaction(transaction);
    }

    private sealed class EfCoreOrderTransaction : IOrderTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfCoreOrderTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken) => _transaction.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }

    public Task<bool> AddressBelongsToUserAsync(string addressId, string userId, CancellationToken cancellationToken)
    {
        return _dbContext.Addresses.AnyAsync(item => item.Id == addressId && item.UserId == userId, cancellationToken);
    }

    public Task<Order?> GetByPaymentReferenceAsync(string reference, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders.Include(item => item.Items).Include(item => item.InventoryReservations);
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.PaymentReference == reference, cancellationToken);
    }

    public async Task<(Order Order, Address? Address, ApplicationUser? User)> GetByPaymentReferenceWithDetailsAsync(string reference, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders
            .Include(item => item.Items)
            .Include(item => item.InventoryReservations)
            .Include(item => item.User);
        if (!trackChanges) query = query.AsNoTracking();

        var order = await query.SingleOrDefaultAsync(item => item.PaymentReference == reference, cancellationToken);
        if (order is null) return (null!, null, null);

        var address = await _dbContext.Addresses.AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == order.AddressId, cancellationToken);

        return (order, address, order.User);
    }

    public Task<Order?> GetByIdempotencyKeyAsync(string userId, string idempotencyKey, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders.Include(item => item.Items).Include(item => item.InventoryReservations);
        if (!trackChanges) query = query.AsNoTracking();
        return query.SingleOrDefaultAsync(item => item.UserId == userId && item.IdempotencyKey == idempotencyKey, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryReservation>> GetExpiredReservationsAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        return await _dbContext.InventoryReservations.Where(item =>
            item.Status == FashionStore.Domain.Constants.InventoryReservationStatuses.Reserved && item.ExpiresAt <= now)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task CreateWithInventoryReservationsAsync(Order order, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var reservationJson = JsonSerializer.Serialize(order.Items
            .GroupBy(item => item.ProductId)
            .Select(group => new { productId = group.Key, quantity = group.Sum(item => item.Quantity) }));

        return _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                _dbContext.Orders.Add(order);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await _dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                    SELECT reserve_order_inventory(
                        {order.Id},
                        CAST({reservationJson} AS jsonb),
                        {expiresAt});
                    """, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public async Task<bool> ReleaseInventoryReservationAsync(string reservationId, string status, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT release_inventory_reservation(@reservationId, @status);";

        var pId = command.CreateParameter();
        pId.ParameterName = "reservationId";
        pId.Value = reservationId;
        command.Parameters.Add(pId);

        var pStatus = command.CreateParameter();
        pStatus.ParameterName = "status";
        pStatus.Value = status;
        command.Parameters.Add(pStatus);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool boolResult && boolResult;
    }

    public async Task<bool> ConsumeInventoryReservationAsync(string reservationId, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.Transaction = _dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT consume_inventory_reservation(@reservationId);";

        var pId = command.CreateParameter();
        pId.ParameterName = "reservationId";
        pId.Value = reservationId;
        command.Parameters.Add(pId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool boolResult && boolResult;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
