using FashionStore.API.Features.Payments.Shared;

namespace FashionStore.API.Features.Payments.VerifyPaystack;

public interface IVerifyPaystackService
{
    Task<ResponseResult<PaymentVerificationResponse>> ExecuteAsync(string reference, string? userId, CancellationToken cancellationToken);
}
