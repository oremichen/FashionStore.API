using FashionStore.Domain.Abstractions.Contacts;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.ContactUsRepo;

public sealed class ContactUsRepository(FashionStoreDbContext dbContext) : IContactUsRepository
{
    public async Task<IReadOnlyList<ContactUs>> GetAllAsync(bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.ContactUs : dbContext.ContactUs.AsNoTracking();
        return await query.OrderByDescending(contact => contact.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<ContactUs?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = trackChanges ? dbContext.ContactUs : dbContext.ContactUs.AsNoTracking();
        return query.SingleOrDefaultAsync(contact => contact.Id == id, cancellationToken);
    }

    public Task<ContactUs?> GetActiveAsync(CancellationToken cancellationToken)
    {
        return dbContext.ContactUs.AsNoTracking()
            .SingleOrDefaultAsync(contact => contact.IsActive, cancellationToken);
    }

    public async Task AddAsync(ContactUs contact, CancellationToken cancellationToken)
    {
        await dbContext.ContactUs.AddAsync(contact, cancellationToken);
    }

    public Task DeleteAsync(ContactUs contact, CancellationToken cancellationToken)
    {
        dbContext.ContactUs.Remove(contact);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
