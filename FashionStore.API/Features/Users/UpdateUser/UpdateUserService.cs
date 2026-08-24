namespace FashionStore.API.Features.Users.UpdateUser;

public sealed class UpdateUserService(UserOperations operations) : IUpdateUserService
{
    public Task<ResponseResult<UserDetailsResponse>> ExecuteAsync(UpdateUserDetailsRequest request) => operations.UpdateUserDetails(request);
}
