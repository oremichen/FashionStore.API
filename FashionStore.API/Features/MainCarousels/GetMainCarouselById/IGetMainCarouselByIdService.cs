namespace FashionStore.API.Features.MainCarousels.GetMainCarouselById;

public interface IGetMainCarouselByIdService
{
    Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(string id, CancellationToken cancellationToken);
}
