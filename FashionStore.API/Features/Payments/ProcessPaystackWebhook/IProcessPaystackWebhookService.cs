namespace FashionStore.API.Features.Payments.ProcessPaystackWebhook;

public interface IProcessPaystackWebhookService
{
    Task<ResponseResult> ExecuteAsync(string payload, string signature, CancellationToken cancellationToken);
}
