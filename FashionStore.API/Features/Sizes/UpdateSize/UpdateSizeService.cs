using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Sizes.UpdateSize;

public sealed class UpdateSizeService(ICatalogOptionRepository repository) : IUpdateSizeService
{
    public async Task<ResponseResult<SizeResponse>> ExecuteAsync(string id, UpdateSizeRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<SizeResponse>();
        if (string.IsNullOrWhiteSpace(id))
            return response.Fail("Size id is required.", ResponseCodes.INVALID_ACTION);
        var sizeId = id.Trim();
        var size = await repository.GetSizeByIdAsync(sizeId, cancellationToken);
        if (size is null)
            return response.Fail("Size was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        if (await repository.SizeNameExistsAsync(request.Name, sizeId, cancellationToken))
            return response.Fail("A size with this name already exists.", ResponseCodes.DUPLICATE_RECORD);

        try
        {
            size.Update(request.Name, request.DisplayName, request.SortOrder);
            await repository.SaveChangesAsync(cancellationToken);
            return response.Success(new SizeResponse
            {
                Id = size.Id, Name = size.Name, DisplayName = size.DisplayName,
                SortOrder = size.SortOrder, IsActive = size.IsActive
            }, "Size updated successfully.");
        }
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }
}
