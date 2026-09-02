namespace FashionStore.API.Features.Users.UpdateUser;

public interface IUpdateUserService
{
    Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(string userId, UpdateUserDetailsRequest request, CancellationToken cancellationToken);
}
