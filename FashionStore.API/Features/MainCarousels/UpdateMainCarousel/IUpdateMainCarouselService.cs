namespace FashionStore.API.Features.MainCarousels.UpdateMainCarousel;

public interface IUpdateMainCarouselService
{
    Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(string id, UpdateMainCarouselRequest request, CancellationToken cancellationToken);
}
