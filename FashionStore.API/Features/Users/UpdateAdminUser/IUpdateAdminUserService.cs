namespace FashionStore.API.Features.Users.UpdateAdminUser;
public interface IUpdateAdminUserService
{
    Task<ResponseResult> ExecuteAsync(string actorId, string userId, UpdateAdminUserRequest request);
}
