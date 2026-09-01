namespace FashionStore.API.Features.Users.ResetAdminPassword;

public interface IResetAdminPasswordService
{
    Task<ResponseResult> ExecuteAsync(string userId, CancellationToken cancellationToken);
}
