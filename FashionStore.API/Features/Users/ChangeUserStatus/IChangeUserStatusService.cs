namespace FashionStore.API.Features.Users.ChangeUserStatus;

public interface IChangeUserStatusService
{
    Task<ResponseResult> ExecuteAsync(string userId, ChangeUserStatusRequest request, CancellationToken cancellationToken);
}
