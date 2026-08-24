namespace FashionStore.API.Features.MainCarousels.DeleteMainCarousel;

public interface IDeleteMainCarouselService
{
    Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken);
}
