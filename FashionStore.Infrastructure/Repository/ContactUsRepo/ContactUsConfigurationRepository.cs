using FashionStore.Domain.Abstractions.Contacts;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.ContactUsRepo;

public sealed class ContactUsConfigurationRepository(FashionStoreDbContext dbContext) : IContactUsConfigurationRepository
{
    public async Task<IReadOnlyList<ContactUsConfiguration>> GetAllAsync(bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.ContactUsConfigurations : dbContext.ContactUsConfigurations.AsNoTracking();
        return await query.OrderByDescending(contact => contact.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<ContactUsConfiguration?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.ContactUsConfigurations : dbContext.ContactUsConfigurations.AsNoTracking();
        return query.SingleOrDefaultAsync(contact => contact.Id == id, cancellationToken);
    }

    public Task<ContactUsConfiguration?> GetActiveAsync(CancellationToken cancellationToken)
    {
        return dbContext.ContactUsConfigurations.AsNoTracking()
            .SingleOrDefaultAsync(contact => contact.IsActive, cancellationToken);
    }

    public async Task AddAsync(ContactUsConfiguration contact, CancellationToken cancellationToken)
    {
        await dbContext.ContactUsConfigurations.AddAsync(contact, cancellationToken);
    }

    public Task DeleteAsync(ContactUsConfiguration contact, CancellationToken cancellationToken)
    {
        dbContext.ContactUsConfigurations.Remove(contact);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
