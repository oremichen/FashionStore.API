namespace FashionStore.API.Features.MainCarousels.UpdateMainCarousel;

public sealed class UpdateMainCarouselService(MainCarouselOperations operations) : IUpdateMainCarouselService
{
    public Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(string id, UpdateMainCarouselRequest request, CancellationToken cancellationToken) => operations.UpdateAsync(id, request, cancellationToken);
}
