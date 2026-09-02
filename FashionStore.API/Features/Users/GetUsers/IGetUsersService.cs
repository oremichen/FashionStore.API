namespace FashionStore.API.Features.Users.GetUsers;

public interface IGetUsersService
{
    Task<ResponseResult<PagedResponse<GetUsersResponse>>> ExecuteAsync(GetUsersQuery query, CancellationToken cancellationToken);
}
