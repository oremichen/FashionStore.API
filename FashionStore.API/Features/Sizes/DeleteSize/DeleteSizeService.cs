using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Sizes.DeleteSize;
public sealed class DeleteSizeService(ICatalogOptionRepository repository) : IDeleteSizeService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Size id is required.", ResponseCodes.INVALID_ACTION);
        var sizeId = id.Trim();
        var size = await repository.GetSizeByIdAsync(sizeId, cancellationToken);
        if (size is null)
            return response.Fail("Size was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.SizeHasProductsAsync(sizeId, cancellationToken))
            return response.Fail("Size cannot be deleted because it is already mapped to a product.", ResponseCodes.INVALID_ACTION);
        await repository.DeleteSizeAsync(size, cancellationToken);
        return response.Success("Size deleted successfully.");
    }
}
