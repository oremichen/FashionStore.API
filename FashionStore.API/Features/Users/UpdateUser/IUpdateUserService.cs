namespace FashionStore.API.Features.Users.UpdateUser;

public interface IUpdateUserService
{
    Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(UpdateUserDetailsRequest request);
}
