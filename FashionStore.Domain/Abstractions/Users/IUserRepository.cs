using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Users;

public enum UserRepositoryCategory { Customer, Admin }
public enum UserRepositoryStatusFilter { All, Active, Deactivated, Deleted }

public interface IUserRepository
{
    Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Address>> GetSystemAddressesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Address>> GetAddressesAsync(string userId, bool trackChanges, CancellationToken cancellationToken);
    Task<Address?> GetAddressAsync(string userId, string addressId, CancellationToken cancellationToken);
    void AddAddress(Address address);
    void DeleteAddress(Address address);
    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount, IReadOnlyDictionary<string, IReadOnlyList<string>> RolesByUser)> GetPagedUsersAsync(
        UserRepositoryCategory category,
        UserRepositoryStatusFilter status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
