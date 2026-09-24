using FashionStore.Domain.Abstractions.Types;

namespace FashionStore.API.Features.Types.UpdateType;

public sealed class UpdateTypeService(ITypeRepository repository, ILogger<UpdateTypeService> logger) : IUpdateTypeService
{
    public async Task<ResponseResult<TypeResponse>> ExecuteAsync(string id, UpdateTypeRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating type {TypeId}.", id);
        var response = new ResponseResult<TypeResponse>();
        if (string.IsNullOrWhiteSpace(id))
        {
            return response.Fail("Type id is required.", ResponseCodes.INVALID_ACTION);
        }

        var typeId = id.Trim();
        var type = await repository.GetByIdAsync(typeId, cancellationToken);
        if (type is null)
        {
            logger.LogWarning("Type {TypeId} was not found for update.", typeId);
            return response.Fail("Type was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        if (await repository.NameOrSlugExistsAsync(request.Name, request.Slug, cancellationToken, typeId))
        {
            logger.LogWarning("Type update rejected because name or slug already exists. TypeId: {TypeId}.", typeId);
            return response.Fail("A type with this name or slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        }

        try
        {
            type.SetDetails(request.Name, request.Slug, request.Description, request.IsActive);
            await repository.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Updated type {TypeId}.", typeId);
            return response.Success(TypeResponseMapper.Map(type), "Type updated successfully.");
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "Type update validation failed for {TypeId}.", typeId);
            return response.Fail("The type details are invalid.", ResponseCodes.INVALID_ACTION);
        }
    }
}
