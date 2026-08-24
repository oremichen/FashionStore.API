namespace FashionStore.API.Features.Users.CreateUser;

public interface ICreateUserService
{
    Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(CreateUserRequest request);
}
