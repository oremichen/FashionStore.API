namespace FashionStore.API.Features.Delivery.DeleteRate;

public interface IDeleteRateService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
