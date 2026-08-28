namespace FashionStore.API.Features.MainCarousels.GetMainCarousels;

public interface IGetMainCarouselsService
{
    Task<ResponseResult<IReadOnlyList<MainCarouselResponse>>> ExecuteAsync(CancellationToken cancellationToken);
}
