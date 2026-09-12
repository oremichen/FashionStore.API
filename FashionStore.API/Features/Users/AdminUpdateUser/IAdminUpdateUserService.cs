namespace FashionStore.API.Features.Users.AdminUpdateUser;
public interface IAdminUpdateUserService
{
    Task<ResponseResult> ExecuteAsync(string actorId, string userId, AdminUpdateUserRequest request, CancellationToken cancellationToken);
}
