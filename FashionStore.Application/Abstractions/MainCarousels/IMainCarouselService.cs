namespace FashionStore.Application.Abstractions.MainCarousels;

public interface IMainCarouselService
{
    Task<ResponseResult<IReadOnlyList<MainCarouselResponse>>> GetAllAsync(CancellationToken cancellationToken);
    Task<ResponseResult<MainCarouselResponse>> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<ResponseResult<MainCarouselResponse>> CreateAsync(CreateMainCarouselRequest request, CancellationToken cancellationToken);
    Task<ResponseResult<MainCarouselResponse>> UpdateAsync(string id, UpdateMainCarouselRequest request, CancellationToken cancellationToken);
    Task<ResponseResult> DeleteAsync(string id, CancellationToken cancellationToken);
}
