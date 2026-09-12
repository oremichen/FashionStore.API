namespace FashionStore.API.Features.Users.UploadProfilePicture;
public interface IUploadProfilePictureService
{
    Task<ResponseResult<UpdateProfilePictureResponse>> ExecuteAsync(string userId, IFormFile file, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string userId, CancellationToken cancellationToken);
}
