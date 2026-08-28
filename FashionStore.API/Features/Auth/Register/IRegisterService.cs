namespace FashionStore.API.Features.Auth.Register;

public interface IRegisterService
{
    Task<ResponseResult> ExecuteAsync(RegisterRequest request);
}
