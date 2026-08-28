using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.Infrastructure.Repository.ContactRepo;

public sealed class ContactUsRepository(FashionStoreDbContext dbContext) : IContactUsRepository
{
    public async Task AddAsync(ContactUs contact, CancellationToken cancellationToken) =>
        await dbContext.ContactUs.AddAsync(contact, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
