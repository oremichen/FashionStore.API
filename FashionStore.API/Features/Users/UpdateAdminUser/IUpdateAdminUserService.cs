namespace FashionStore.API.Features.Users.UpdateAdminUser;
public interface IUpdateAdminUserService
{
    Task<ResponseResult> ExecuteAsync(string userId, UpdateAdminUserRequest request);
}
