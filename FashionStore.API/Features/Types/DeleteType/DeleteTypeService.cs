using FashionStore.Domain.Abstractions.Types;

namespace FashionStore.API.Features.Types.DeleteType;

public sealed class DeleteTypeService(ITypeRepository repository, ILogger<DeleteTypeService> logger) : IDeleteTypeService
{
    public async Task<ResponseResult> ExecuteAsync(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting type {TypeId}.", id);
        var response = new ResponseResult();
        if (string.IsNullOrWhiteSpace(id))
        {
            return response.Fail("Type id is required.", ResponseCodes.INVALID_ACTION);
        }

        var typeId = id.Trim();
        var type = await repository.GetByIdAsync(typeId, cancellationToken);
        if (type is null)
        {
            logger.LogWarning("Type {TypeId} was not found for deletion.", typeId);
            return response.Fail("Type was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        if (await repository.HasProductsAsync(typeId, cancellationToken))
        {
            logger.LogWarning("Type {TypeId} cannot be deleted because it is mapped to products.", typeId);
            return response.Fail("Type cannot be deleted because it is already mapped to a product.", ResponseCodes.INVALID_ACTION);
        }

        await repository.DeleteAsync(type, cancellationToken);
        logger.LogInformation("Deleted type {TypeId}.", typeId);
        return response.Success("Type deleted successfully.");
    }
}
