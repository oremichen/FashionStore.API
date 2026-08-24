using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Contacts;

public interface IContactUsRepository
{
    Task<IReadOnlyList<ContactUs>> GetAllAsync(bool trackChanges, CancellationToken cancellationToken);
    Task<ContactUs?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task<ContactUs?> GetActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(ContactUs contact, CancellationToken cancellationToken);
    Task DeleteAsync(ContactUs contact, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
