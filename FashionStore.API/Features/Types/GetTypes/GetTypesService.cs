using FashionStore.Domain.Abstractions.Types;

namespace FashionStore.API.Features.Types.GetTypes;

public sealed class GetTypesService(ITypeRepository repository, ILogger<GetTypesService> logger) : IGetTypesService
{
    public async Task<ResponseResult<IReadOnlyList<TypeResponse>>> ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving active types.");
        var types = await repository.GetAllAsync(true, cancellationToken);
        var responses = types.Select(TypeResponseMapper.Map).ToList();
        logger.LogInformation("Retrieved {TypeCount} active types.", responses.Count);
        return new ResponseResult<IReadOnlyList<TypeResponse>>().Success(responses, "Types retrieved successfully.");
    }
}
