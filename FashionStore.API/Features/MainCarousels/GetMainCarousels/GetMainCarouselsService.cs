namespace FashionStore.API.Features.MainCarousels.GetMainCarousels;

public sealed class GetMainCarouselsService(MainCarouselOperations operations) : IGetMainCarouselsService
{
    public Task<ResponseResult<IReadOnlyList<MainCarouselResponse>>> ExecuteAsync(CancellationToken cancellationToken) => operations.GetAllAsync(cancellationToken);
}
