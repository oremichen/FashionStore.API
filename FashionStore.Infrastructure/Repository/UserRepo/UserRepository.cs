using FashionStore.Domain.Abstractions.Users;
using FashionStore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FashionStore.Infrastructure.Repository.UserRepo;

public sealed class UserRepository(FashionStoreDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsAsync(string userId, CancellationToken cancellationToken)
    {
        return dbContext.Users.AnyAsync(user => user.Id == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Address>> GetAddressesAsync(string userId, bool trackChanges, CancellationToken cancellationToken)
    {
        var query = dbContext.Addresses.Where(address => address.UserId == userId);
        if (!trackChanges) query = query.AsNoTracking();
        return await query.OrderByDescending(address => address.IsMain).ThenBy(address => address.Id).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Address>> GetSystemAddressesAsync(CancellationToken cancellationToken)
    {
        var query = dbContext.Addresses.Where(address => string.IsNullOrEmpty(address.UserId)).AsNoTracking();
        return await query.ToListAsync(cancellationToken);
    }

    public Task<Address?> GetAddressAsync(string userId, string addressId, CancellationToken cancellationToken)
    {
        return dbContext.Addresses.SingleOrDefaultAsync(address => address.UserId == userId && address.Id == addressId, cancellationToken);
    }

    public void AddAddress(Address address)
    {
        dbContext.Addresses.Add(address);
    }

    public void DeleteAddress(Address address)
    {
        dbContext.Addresses.Remove(address);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<ApplicationUser> Items, int TotalCount, IReadOnlyDictionary<string, IReadOnlyList<string>> RolesByUser)> GetPagedUsersAsync(
        UserRepositoryCategory category,
        UserRepositoryStatusFilter status,
        DateTimeOffset? from,
        DateTimeOffset? to,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var users = dbContext.Users.AsNoTracking().AsQueryable();
        var adminRoles = RoleConstants.AdminRoles.ToArray();

        if (category == UserRepositoryCategory.Customer)
        {
            users = users.Where(user =>
                dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id &&
                    dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == RoleConstants.User)));
        }
        else
        {
            users = users.Where(user =>
                dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id &&
                    dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name != null && adminRoles.Contains(role.Name))));
        }

        users = status switch
        {
            UserRepositoryStatusFilter.Active => users.Where(user => !user.IsDeleted && !user.IsDeactivated),
            UserRepositoryStatusFilter.Deactivated => users.Where(user => !user.IsDeleted && user.IsDeactivated),
            UserRepositoryStatusFilter.Deleted => users.Where(user => user.IsDeleted),
            _ => users
        };

        if (from.HasValue)
            users = users.Where(user => user.CreatedAt >= from.Value);

        if (to.HasValue)
            users = users.Where(user => user.CreatedAt <= to.Value);

        var normalizedSearch = search?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            users = users.Where(user =>
                user.FirstName.ToLower().Contains(normalizedSearch) ||
                user.LastName.ToLower().Contains(normalizedSearch) ||
                (user.Email != null && user.Email.ToLower().Contains(normalizedSearch)) ||
                (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(normalizedSearch)) ||
                user.Addresses.Any(address =>
                    address.Street.ToLower().Contains(normalizedSearch) ||
                    address.City.ToLower().Contains(normalizedSearch) ||
                    address.State.ToLower().Contains(normalizedSearch) ||
                    address.Country.ToLower().Contains(normalizedSearch) ||
                    (address.PostalCode != null && address.PostalCode.ToLower().Contains(normalizedSearch)) ||
                    address.PhoneNumber.ToLower().Contains(normalizedSearch) ||
                    (address.Landmark != null && address.Landmark.ToLower().Contains(normalizedSearch))));
        }

        var totalCount = await users.CountAsync(cancellationToken);

        var pageUsers = await users
            .OrderByDescending(user => user.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(user => user.Addresses)
            .ToListAsync(cancellationToken);

        var userIds = pageUsers.Select(user => user.Id).ToArray();

        var roleRows = await (
            from userRole in dbContext.UserRoles.AsNoTracking()
            join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new { userRole.UserId, Role = role.Name! })
            .ToListAsync(cancellationToken);

        var rolesByUser = roleRows
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(item => item.Role).Order().ToList());

        return (pageUsers, totalCount, rolesByUser);
    }
}
