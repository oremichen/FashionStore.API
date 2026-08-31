using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Colors.GetColors;
public sealed class GetColorsService(ICatalogOptionRepository repository) : IGetColorsService
{
    public async Task<ResponseResult<PagedResponse<ColorResponse>>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PagedResponse<ColorResponse>>();
        if (page < 1 || pageSize is < 1 or > 100)
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);
        var result = await repository.GetColorsAsync(page, pageSize, cancellationToken);
        return response.Success(new PagedResponse<ColorResponse>
        {
            Items = result.Items.Select(MapColor).ToList(), Page = page, PageSize = pageSize,
            TotalCount = result.TotalCount, TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize)
        }, "Colors retrieved successfully.");
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
