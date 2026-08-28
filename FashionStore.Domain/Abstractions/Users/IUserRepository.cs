using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Users;

public interface IUserRepository
{
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Address>> GetAddressesAsync(string userId, bool trackChanges, CancellationToken cancellationToken);
    Task<Address?> GetAddressAsync(string userId, string addressId, CancellationToken cancellationToken);
    void AddAddress(Address address);
    void DeleteAddress(Address address);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
