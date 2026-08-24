namespace FashionStore.API.Features.Auth.Register;

public sealed class RegisterService(AuthOperations operations) : IRegisterService
{
    public Task<ResponseResult> ExecuteAsync(RegisterRequest request) => operations.Register(request);
}
