namespace FashionStore.API.Features.Users.GetUserByEmail;

public sealed class GetUserByEmailService(UserOperations operations) : IGetUserByEmailService
{
    public Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(string email) => operations.GetUserByEmail(email);
}
