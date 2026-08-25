using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Contacts;

public interface IContactUsConfigurationRepository
{
    Task<IReadOnlyList<ContactUsConfiguration>> GetAllAsync(bool trackChanges, CancellationToken cancellationToken);
    Task<ContactUsConfiguration?> GetByIdAsync(string id, bool trackChanges, CancellationToken cancellationToken);
    Task<ContactUsConfiguration?> GetActiveAsync(CancellationToken cancellationToken);
    Task AddAsync(ContactUsConfiguration contact, CancellationToken cancellationToken);
    Task DeleteAsync(ContactUsConfiguration contact, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
