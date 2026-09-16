using FashionStore.Domain.Abstractions.Users;

namespace FashionStore.API.Features.Users.GetUsers;

public sealed class GetUsersService(IUserRepository userRepository) : IGetUsersService
{
    public async Task<ResponseResult<PagedResponse<GetUsersResponse>>> ExecuteAsync(GetUsersQuery query, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PagedResponse<GetUsersResponse>>();
        if (query.Page < 1 || query.PageSize is < 1 or > 100)
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);
        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            return response.Fail("The from date cannot be later than the to date.", ResponseCodes.INVALID_ACTION);

        var repoCategory = query.Category == UserCategory.Customer
            ? UserRepositoryCategory.Customer
            : UserRepositoryCategory.Admin;

        var repoStatus = query.Status switch
        {
            UserStatusFilter.Active => UserRepositoryStatusFilter.Active,
            UserStatusFilter.Deactivated => UserRepositoryStatusFilter.Deactivated,
            UserStatusFilter.Deleted => UserRepositoryStatusFilter.Deleted,
            _ => UserRepositoryStatusFilter.All
        };

        var result = await userRepository.GetPagedUsersAsync(
            repoCategory,
            repoStatus,
            query.From,
            query.To,
            query.Search,
            query.Page,
            query.PageSize,
            cancellationToken);

        var items = result.Items.Select(user => new GetUsersResponse
        {
            Id = user.Id,
            Roles = result.RolesByUser.GetValueOrDefault(user.Id, []),
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
            Items = items, Page = query.Page, PageSize = query.PageSize, TotalCount = result.TotalCount,
            TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize)
        }, "Users retrieved successfully.");
    }
}
