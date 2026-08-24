namespace FashionStore.API.Features.MainCarousels.GetMainCarouselById;

public sealed class GetMainCarouselByIdService(MainCarouselOperations operations) : IGetMainCarouselByIdService
{
    public Task<ResponseResult<MainCarouselResponse>> ExecuteAsync(string id, CancellationToken cancellationToken) => operations.GetByIdAsync(id, cancellationToken);
}
