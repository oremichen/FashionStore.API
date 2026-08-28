using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Contacts;

public interface IContactUsRepository
{
    Task AddAsync(ContactUs contact, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
