using FashionStore.Domain.Abstractions.CatalogOptions;

namespace FashionStore.API.Features.Colors.CreateColor;
public sealed class CreateColorService(ICatalogOptionRepository repository) : ICreateColorService
{
    public async Task<ResponseResult<ColorResponse>> ExecuteAsync(CreateColorRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<ColorResponse>();
        if (await repository.ColorNameExistsAsync(request.Name, cancellationToken))
        {
            return response.Fail("A color with this name already exists.", ResponseCodes.DUPLICATE_RECORD);
        }

        try
        {
            var color = Color.Create(request.Name, request.HexCode, request.SortOrder);
            await repository.AddColorAsync(color, cancellationToken);
            return response.Success(MapColor(color), "Color created successfully.").SetStatusCode(ResponseCodes.CREATED);
        }
        catch (ArgumentException exception)
        {
            return response.Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }
    }

    private static ColorResponse MapColor(Color color)
    {
        return new ColorResponse
        {
            Id = color.Id,
            Name = color.Name,
            HexCode = color.HexCode,
            SortOrder = color.SortOrder,
            IsActive = color.IsActive
        };
    }
}
