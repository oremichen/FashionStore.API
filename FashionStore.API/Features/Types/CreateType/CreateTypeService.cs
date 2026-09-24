using FashionStore.Domain.Abstractions.Types;

namespace FashionStore.API.Features.Types.CreateType;

public sealed class CreateTypeService(ITypeRepository repository, ILogger<CreateTypeService> logger) : ICreateTypeService
{
    public async Task<ResponseResult<TypeResponse>> ExecuteAsync(CreateTypeRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating type with slug {Slug}.", request.Slug);
        var response = new ResponseResult<TypeResponse>();
        if (await repository.NameOrSlugExistsAsync(request.Name, request.Slug, cancellationToken))
        {
            logger.LogError("Type creation rejected because name or slug already exists. Slug: {Slug}.", request.Slug);
            return response.Fail("A type with this name or slug already exists.", ResponseCodes.DUPLICATE_RECORD);
        }

        try
        {
            var type = FashionStore.Domain.Entities.Type.Create(request.Name, request.Slug, request.Description, request.IsActive);
            await repository.AddAsync(type, cancellationToken);
            logger.LogInformation("Created type {TypeId}.", type.Id);
            return response.Success(TypeResponseMapper.Map(type), "Type created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception)
        {
            logger.LogError(exception, "Type creation validation failed for slug {Slug}.", request.Slug);
            return response.Fail("The type details are invalid.", ResponseCodes.INVALID_ACTION);
        }
    }
}
