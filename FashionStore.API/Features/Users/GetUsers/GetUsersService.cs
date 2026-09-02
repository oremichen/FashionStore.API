namespace FashionStore.API.Features.Users.GetUsers;

public sealed class GetUsersService(FashionStoreDbContext dbContext) : IGetUsersService
{
    public async Task<ResponseResult<PagedResponse<GetUsersResponse>>> ExecuteAsync(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PagedResponse<GetUsersResponse>>();
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);
        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            return response.Fail("The from date cannot be later than the to date.", ResponseCodes.INVALID_ACTION);

        var users = dbContext.Users.AsNoTracking().AsQueryable();
        var adminRoles = RoleConstants.AdminRoles.ToArray();
        users = query.Category == UserCategory.Customer
            ? users.Where(user => dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id &&
                dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name == RoleConstants.User)))
            : users.Where(user => dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id &&
                dbContext.Roles.Any(role => role.Id == userRole.RoleId && role.Name != null && adminRoles.Contains(role.Name))));

        users = query.Status switch
        {
            UserStatusFilter.Active => users.Where(user => !user.IsDeleted && !user.IsDeactivated),
            UserStatusFilter.Deactivated => users.Where(user => !user.IsDeleted && user.IsDeactivated),
            UserStatusFilter.Deleted => users.Where(user => user.IsDeleted),
            _ => users
        };

        if (query.From.HasValue) users = users.Where(user => user.CreatedAt >= query.From.Value);
        if (query.To.HasValue) users = users.Where(user => user.CreatedAt <= query.To.Value);

        var search = query.Search?.Trim().ToLower();
        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(user =>
                user.FirstName.ToLower().Contains(search) ||
                user.LastName.ToLower().Contains(search) ||
                (user.Email != null && user.Email.ToLower().Contains(search)) ||
                (user.PhoneNumber != null && user.PhoneNumber.ToLower().Contains(search)) ||
                user.Addresses.Any(address =>
                    address.Street.ToLower().Contains(search) || address.City.ToLower().Contains(search) ||
                    address.State.ToLower().Contains(search) || address.Country.ToLower().Contains(search) ||
                    (address.PostalCode != null && address.PostalCode.ToLower().Contains(search)) ||
                    address.PhoneNumber.ToLower().Contains(search) ||
                    address.Landmark != null && address.Landmark.ToLower().Contains(search)));
        }

        var totalCount = await users.CountAsync(cancellationToken);
        var pageUsers = await users.OrderByDescending(user => user.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)
            .Include(user => user.Addresses).ToListAsync(cancellationToken);
        var userIds = pageUsers.Select(user => user.Id).ToArray();
        var roleRows = await (from userRole in dbContext.UserRoles.AsNoTracking()
                              join role in dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                              where userIds.Contains(userRole.UserId)
                              select new { userRole.UserId, Role = role.Name! }).ToListAsync(cancellationToken);
        var rolesByUser = roleRows.GroupBy(item => item.UserId)
            .ToDictionary(group => group.Key, group => (IReadOnlyList<string>)group.Select(item => item.Role).Order().ToList());

        var items = pageUsers.Select(user => new GetUsersResponse
        {
            Id = user.Id,
            Roles = rolesByUser.GetValueOrDefault(user.Id, []),
            FirstName = user.FirstName,
            LastName = user.LastName,
            Addresses = user.Addresses.OrderByDescending(address => address.IsMain).Select(UserAddressResponse.From).ToList(),
            PhoneNumber = user.PhoneNumber,
            Email = user.Email ?? string.Empty,
            DateJoined = user.CreatedAt,
            IsActiveStatus = !user.IsDeleted && !user.IsDeactivated,
            IsDeletedStatus = user.IsDeleted
        }).ToList();

        return response.Success(new PagedResponse<GetUsersResponse>
        {
            Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = totalCount,
            TotalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)query.PageSize)
        }, "Users retrieved successfully.");
    }
}
