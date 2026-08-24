using FashionStore.Domain.Abstractions.Users;
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
}
