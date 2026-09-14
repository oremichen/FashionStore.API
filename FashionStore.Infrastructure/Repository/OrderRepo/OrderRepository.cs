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
        return await _dbContext.InventoryReservations
            .Include(item => item.Order)
            .Where(item =>
                item.Status == FashionStore.Domain.Constants.InventoryReservationStatuses.Reserved && item.ExpiresAt <= now)
            .AsNoTracking()
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

    public Task<TResult> ExecuteInRetriableTransactionAsync<TResult>(
        Func<IOrderTransaction, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        return _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async (ct) =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            var orderTransaction = new EfCoreOrderTransaction(transaction);
            try
            {
                var result = await operation(orderTransaction, ct);
                return result;
            }
            catch
            {
                if (transaction.GetDbTransaction()?.Connection is not null)
                {
                    try { await transaction.RollbackAsync(ct); } catch { /* best-effort rollback; strategy will retry */ }
                }
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        }, cancellationToken);
    }

    public Task<Address?> GetAddressByIdAsync(string addressId, CancellationToken cancellationToken)
    {
        return _dbContext.Addresses
            .AsNoTracking()
            .SingleOrDefaultAsync(address => address.Id == addressId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Address>> GetAddressesByIdsAsync(IEnumerable<string> addressIds, CancellationToken cancellationToken)
    {
        var ids = addressIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<string, Address>();

        return await _dbContext.Addresses
            .AsNoTracking()
            .Where(address => ids.Contains(address.Id))
            .ToDictionaryAsync(address => address.Id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, string?>> GetPrimaryProductImagesAsync(IEnumerable<string> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<string, string?>();

        var result = await (
            from product in _dbContext.Products
            from image in product.Images
            where ids.Contains(product.Id) && image.IsPrimary
            select new { product.Id, image.SmallUrl, image.MediumUrl, image.BigUrl })
            .ToDictionaryAsync(
                item => item.Id,
                item => (string?)(item.SmallUrl ?? item.MediumUrl ?? item.BigUrl),
                cancellationToken);

        return result;
    }

    public Task<Order?> GetOrderByIdWithDetailsAsync(string orderId, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders
            .Include(item => item.User)
            .Include(item => item.Items);

        if (!trackChanges)
            query = query.AsNoTracking();

        return query.SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken);
    }

    public async Task<(Order Order, Address? Address, IReadOnlyDictionary<string, string?> ProductImages)?> GetOrderByIdWithResponseDetailsAsync(
        string orderId,
        string? userId,
        bool admin,
        CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders
            .Include(item => item.User)
            .Include(item => item.Items)
            .AsNoTracking();

        if (!admin)
            query = query.Where(item => item.UserId == userId);

        var order = await query.SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken);
        if (order is null)
            return null;

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var productImages = await GetPrimaryProductImagesAsync(productIds, cancellationToken);
        var address = await GetAddressByIdAsync(order.AddressId, cancellationToken);

        return (order, address, productImages);
    }

    public async Task<(Order Order, Address? Address, IReadOnlyDictionary<string, string?> ProductImages)?> GetOrderByIdOrTrackIdWithResponseDetailsAsync(
        string id,
        string? userId,
        bool admin,
        CancellationToken cancellationToken)
    {
        IQueryable<Order> query = _dbContext.Orders
            .Include(item => item.User)
            .Include(item => item.Items)
            .AsNoTracking();

        if (!admin)
            query = query.Where(item => item.UserId == userId);

        var order = await query.SingleOrDefaultAsync(item => item.Id == id || item.TrackOrderId == id, cancellationToken);
        if (order is null)
            return null;

        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var productImages = await GetPrimaryProductImagesAsync(productIds, cancellationToken);
        var address = await GetAddressByIdAsync(order.AddressId, cancellationToken);

        return (order, address, productImages);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount, IReadOnlyDictionary<string, Address> Addresses, IReadOnlyDictionary<string, string?> ProductImages)> GetPagedOrdersAsync(
        string? userId,
        int page,
        int pageSize,
        bool admin,
        string? status,
        string? search,
        DateTimeOffset? from,
        DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var orders = admin ? _dbContext.Orders.AsNoTracking() : _dbContext.Orders.AsNoTracking().Where(item => item.UserId == userId);

        if (!string.IsNullOrWhiteSpace(status))
            orders = orders.Where(item => item.Status == status);

        if (from.HasValue)
            orders = orders.Where(item => item.CreatedAt >= from.Value);

        if (to.HasValue)
            orders = orders.Where(item => item.CreatedAt <= to.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLower();
            orders = orders.Where(item =>
                item.TrackOrderId.ToLower().Contains(normalizedSearch) ||
                item.Email.ToLower().Contains(normalizedSearch) ||
                item.User.FirstName.ToLower().Contains(normalizedSearch) ||
                item.User.LastName.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await orders.CountAsync(cancellationToken);

        var items = await orders
            .Include(item => item.User)
            .Include(item => item.Items)
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var productIds = items.SelectMany(order => order.Items).Select(item => item.ProductId).Distinct().ToList();
        var productImages = await GetPrimaryProductImagesAsync(productIds, cancellationToken);

        var addressIds = items.Select(order => order.AddressId).Distinct().ToList();
        var addresses = await GetAddressesByIdsAsync(addressIds, cancellationToken);

        return (items, totalCount, addresses, productImages);
    }
}
