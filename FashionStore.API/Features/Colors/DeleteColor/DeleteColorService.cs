using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Colors.DeleteColor;
public sealed class DeleteColorService(ICatalogOptionRepository repository) : IDeleteColorService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Color id is required.", ResponseCodes.INVALID_ACTION);
        var colorId = id.Trim();
        var color = await repository.GetColorByIdAsync(colorId, cancellationToken);
        if (color is null)
            return response.Fail("Color was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.ColorHasProductsAsync(colorId, cancellationToken))
            return response.Fail("Color cannot be deleted because it is already mapped to a product.", ResponseCodes.INVALID_ACTION);
        await repository.DeleteColorAsync(color, cancellationToken);
        return response.Success("Color deleted successfully.");
    }
}
