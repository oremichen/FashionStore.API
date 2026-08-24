using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Sizes.CreateSize;
public sealed class CreateSizeService(ICatalogOptionRepository repository) : ICreateSizeService
{
    public async Task<ResponseResult<SizeResponse>> ExecuteAsync(CreateSizeRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<SizeResponse>();
        if (await repository.SizeNameExistsAsync(request.Name, cancellationToken))
        {
            return response.Fail("A size with this name already exists.", ResponseCodes.DUPLICATE_RECORD);
        }

        try
        {
            var size = Size.Create(request.Name, request.DisplayName, request.SortOrder);
            await repository.AddSizeAsync(size, cancellationToken);
            return response.Success(MapSize(size), "Size created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private static SizeResponse MapSize(Size size)
    {
        return new SizeResponse
        {
            Id = size.Id,
            Name = size.Name,
            DisplayName = size.DisplayName,
            SortOrder = size.SortOrder,
            IsActive = size.IsActive
        };
    }
}
