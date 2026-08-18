using FashionStore.Application.Abstractions.CatalogOptions;

namespace FashionStore.Application.Features.CatalogOptions;

public sealed class CatalogOptionService(ICatalogOptionRepository repository) : ICatalogOptionService
{
    public async Task<ResponseResult<IReadOnlyList<SizeResponse>>> GetSizesAsync(CancellationToken cancellationToken)
    {
        var sizes = await repository.GetSizesAsync(cancellationToken);
        return new ResponseResult<IReadOnlyList<SizeResponse>>().Success(sizes.Select(MapSize).ToList(), "Sizes retrieved successfully.");
    }

    public async Task<ResponseResult<SizeResponse>> CreateSizeAsync(CreateSizeRequest request, CancellationToken cancellationToken)
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

    public async Task<ResponseResult> DeleteSizeAsync(string id, CancellationToken cancellationToken)
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

    public async Task<ResponseResult<IReadOnlyList<ColorResponse>>> GetColorsAsync(CancellationToken cancellationToken)
    {
        var colors = await repository.GetColorsAsync(cancellationToken);
        return new ResponseResult<IReadOnlyList<ColorResponse>>().Success(colors.Select(MapColor).ToList(), "Colors retrieved successfully.");
    }

    public async Task<ResponseResult<ColorResponse>> CreateColorAsync(CreateColorRequest request, CancellationToken cancellationToken)
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

    public async Task<ResponseResult> DeleteColorAsync(string id, CancellationToken cancellationToken)
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

    private static SizeResponse MapSize(Size size)
    {
        return new SizeResponse { Id = size.Id, Name = size.Name, DisplayName = size.DisplayName, SortOrder = size.SortOrder, IsActive = size.IsActive };
    }

    private static ColorResponse MapColor(Color color)
    {
        return new ColorResponse { Id = color.Id, Name = color.Name, HexCode = color.HexCode, SortOrder = color.SortOrder, IsActive = color.IsActive };
    }
}
