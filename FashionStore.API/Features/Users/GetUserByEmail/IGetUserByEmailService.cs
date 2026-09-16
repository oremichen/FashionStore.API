namespace FashionStore.API.Features.Users.GetUserByEmail;

public interface IGetUserByEmailService
{
    Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(string userId);
}
