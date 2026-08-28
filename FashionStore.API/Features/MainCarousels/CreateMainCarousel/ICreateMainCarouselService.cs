namespace FashionStore.API.Features.MainCarousels.CreateMainCarousel;

public interface ICreateMainCarouselService
{
    Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(CreateMainCarouselRequest request, CancellationToken cancellationToken);
}
