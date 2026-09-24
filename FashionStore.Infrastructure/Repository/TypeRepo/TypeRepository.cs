using FashionStore.Domain.Abstractions.Types;
using Microsoft.EntityFrameworkCore;
using TypeEntity = FashionStore.Domain.Entities.Type;

namespace FashionStore.Infrastructure.Repository.TypeRepo;

public sealed class TypeRepository(FashionStoreDbContext dbContext, ILogger<TypeRepository> logger) : ITypeRepository
{
    public async Task<(IReadOnlyList<TypeEntity> Items, int TotalCount)> GetAllAsync(int page, int pageSize, bool activeOnly, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying types. ActiveOnly: {ActiveOnly}.", activeOnly);
        var query = dbContext.Types.AsNoTracking();
        if (activeOnly)
        {
            query = query.Where(type => type.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(type => type.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<TypeEntity?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Querying type {TypeId}.", id);
        return dbContext.Types.SingleOrDefaultAsync(type => type.Id == id, cancellationToken);
    }

    public Task<bool> NameOrSlugExistsAsync(string name, string slug, CancellationToken cancellationToken, string? excludedId = null)
    {
        logger.LogDebug("Checking type uniqueness for slug {Slug}.", slug);
        var normalizedName = name.Trim().ToLower();
        var normalizedSlug = slug.Trim().ToLower();
        return dbContext.Types.AnyAsync(type => type.Id != excludedId &&
            (type.Name.ToLower() == normalizedName || type.Slug.ToLower() == normalizedSlug), cancellationToken);
    }

    public Task<bool> HasProductsAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogDebug("Checking whether type {TypeId} is mapped to products.", id);
        return dbContext.Products.AnyAsync(product => product.TypeId == id, cancellationToken);
    }

    public async Task AddAsync(TypeEntity type, CancellationToken cancellationToken)
    {
        logger.LogInformation("Persisting type with slug {Slug}.", type.Slug);
        dbContext.Types.Add(type);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Persisted type {TypeId}.", type.Id);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Saving type changes.");
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(TypeEntity type, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting type {TypeId}.", type.Id);
        dbContext.Types.Remove(type);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Deleted type {TypeId}.", type.Id);
    }
}
