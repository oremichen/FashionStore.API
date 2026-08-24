using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Sizes.GetSizes;
public sealed class GetSizesService(ICatalogOptionRepository repository) : IGetSizesService
{
    public async Task<ResponseResult<IReadOnlyList<SizeResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var sizes = await repository.GetSizesAsync(cancellationToken);
        return new ResponseResult<IReadOnlyList<SizeResponse>>().Success(sizes.Select(MapSize).ToList(), "Sizes retrieved successfully.");
    }

    private static SizeResponse MapSize(Size size)
    {
        return new SizeResponse
        {
            Id = size.Id,
            Name = size.Name,
            DisplayName = size.DisplayName,
            SortOrder = size.SortOrder,
            IsActive = size.IsActive
        };
    }
}
