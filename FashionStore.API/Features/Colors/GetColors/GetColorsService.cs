using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Colors.GetColors;
public sealed class GetColorsService(ICatalogOptionRepository repository) : IGetColorsService
{
    public async Task<ResponseResult<IReadOnlyList<ColorResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        var colors = await repository.GetColorsAsync(cancellationToken);
        return new ResponseResult<IReadOnlyList<ColorResponse>>().Success(colors.Select(MapColor).ToList(), "Colors retrieved successfully.");
    }

    private static ColorResponse MapColor(Color color)
    {
        return new ColorResponse
        {
            Id = color.Id,
            Name = color.Name,
            HexCode = color.HexCode,
            SortOrder = color.SortOrder,
            IsActive = color.IsActive
        };
    }
}
