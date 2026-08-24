namespace FashionStore.API.Features.MainCarousels.DeleteMainCarousel;

public sealed class DeleteMainCarouselService(MainCarouselOperations operations) : IDeleteMainCarouselService
{
    public Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.DeleteAsync(id, cancellationToken);
}
