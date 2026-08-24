namespace FashionStore.API.Features.Users.CreateUser;

public sealed class CreateUserService(UserOperations operations) : ICreateUserService
{
    public Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(CreateUserRequest request) => operations.CreateUser(request);
}
