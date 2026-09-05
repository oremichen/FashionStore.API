using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Sizes.GetSizes;
public sealed class GetSizesService(ICatalogOptionRepository repository) : IGetSizesService
{
    public async Task<ResponseResult<PagedResponse<SizeResponse>>> ExecuteAsync(int page, int pageSize, bool availableOnly, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PagedResponse<SizeResponse>>();
        if (page < 1 || pageSize is < 1 or > 100)
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);
        var result = await repository.GetSizesAsync(page, pageSize, availableOnly, cancellationToken);
        var productCounts = await repository.GetSizeProductCountsAsync(cancellationToken);
        var items = result.Items
            .Select(size => MapSize(size, productCounts.GetValueOrDefault(size.Id)))
            .Where(size => !availableOnly || size.ProductCount > 0)
            .ToList();
        return response.Success(new PagedResponse<SizeResponse>
        {
            Items = items, Page = page, PageSize = pageSize,
            TotalCount = result.TotalCount, TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize)
        }, "Sizes retrieved successfully.");
    }

    private static SizeResponse MapSize(Size size, int productCount)
    {
        return new SizeResponse
        {
            Id = size.Id,
            Name = size.Name,
            DisplayName = size.DisplayName,
            SortOrder = size.SortOrder,
            IsActive = size.IsActive,
            ProductCount = productCount
        };
    }
}
