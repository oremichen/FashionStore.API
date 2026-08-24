namespace FashionStore.API.Features.MainCarousels.CreateMainCarousel;

public sealed class CreateMainCarouselService(MainCarouselOperations operations) : ICreateMainCarouselService
{
    public Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(CreateMainCarouselRequest request, CancellationToken cancellationToken) => operations.CreateAsync(request, cancellationToken);
}
