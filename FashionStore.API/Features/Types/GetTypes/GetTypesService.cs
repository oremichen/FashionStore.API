using FashionStore.Domain.Abstractions.Types;

namespace FashionStore.API.Features.Types.GetTypes;

public sealed class GetTypesService(ITypeRepository repository, ILogger<GetTypesService> logger) : IGetTypesService
{
    public async Task<ResponseResult<PagedResponse<TypeResponse>>> ExecuteAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        logger.LogInformation("Retrieving active types. Page: {Page}, PageSize: {PageSize}.", page, pageSize);
        var response = new ResponseResult<PagedResponse<TypeResponse>>();
        if (page < 1 || pageSize is < 1 or > 100)
        {
            logger.LogWarning("Type retrieval rejected because pagination values are invalid. Page: {Page}, PageSize: {PageSize}.", page, pageSize);
            return response.Fail("Page must be at least 1 and pageSize must be between 1 and 100.", ResponseCodes.INVALID_ACTION);
        }

        var result = await repository.GetAllAsync(page, pageSize, true, cancellationToken);
        var responses = result.Items.Select(TypeResponseMapper.Map).ToList();
        logger.LogInformation("Retrieved {TypeCount} active types from {TotalCount} matching types.", responses.Count, result.TotalCount);
        return response.Success(new PagedResponse<TypeResponse>
        {
            Items = responses,
            Page = page,
            PageSize = pageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalCount == 0 ? 0 : (int)Math.Ceiling(result.TotalCount / (double)pageSize)
        }, "Types retrieved successfully.");
    }
}
