using FashionStore.Application.Abstractions.Products;
using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;

namespace FashionStore.API.Controllers;

[Route("api/products")]
[ApiController]
[Authorize(Roles = "SuperAdmin,BusinessAdmin")]
public sealed class ProductsController(IProductService service) : BaseApiController
{
    [HttpGet("~/api/admin/products")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ProductResponse>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] ProductQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetAsync(query, cancellationToken));
    }

    [HttpGet("~/api/admin/products/{productId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetByIdAsync(productId, cancellationToken));
    }

    [HttpPost("create")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateProductForm form, CancellationToken cancellationToken)
    {
        var images = await ReadImagesAsync(form.Images, cancellationToken);
        var request = new CreateProductRequest(form.CategoryId, form.BrandId, form.Name, form.Slug, form.Description,
            form.ShortDescription, form.OldPrice, form.NewPrice, form.CurrencyCode, form.AvailabilityCount, form.Weight,
            form.WeightUnit, form.IsFeatured, form.IsNewArrival, form.Status, images);
        return ProcessResponse(await service.CreateAsync(request, cancellationToken));
    }

    [HttpPut("update")]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult<ProductResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<IActionResult> Update([FromForm] UpdateProductForm form, CancellationToken cancellationToken)
    {
        var images = await ReadImagesAsync(form.Images, cancellationToken);
        var request = new UpdateProductRequest(form.ProductId, form.CategoryId, form.BrandId, form.Name, form.Slug,
            form.Description, form.ShortDescription, form.OldPrice, form.NewPrice, form.CurrencyCode,
            form.AvailabilityCount, form.Weight, form.WeightUnit, form.IsFeatured, form.IsNewArrival, form.Status, images);
        return ProcessResponse(await service.UpdateAsync(request, cancellationToken));
    }

    [HttpDelete("{productId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.DeleteAsync(productId, cancellationToken));
    }

    [HttpGet("{productId}/images")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ProductImageResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<ProductImageResponse>>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetImages(string productId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetImagesAsync(productId, cancellationToken));
    }

    [AllowAnonymous]
    [HttpGet("{productId}/images/{imageId}/{size}")]
    [Produces("image/jpeg", "image/png", "image/webp", "image/gif")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "image/jpeg", "image/png", "image/webp", "image/gif")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string productId, string imageId, string size, CancellationToken cancellationToken)
    {
        var image = await service.GetImageAsync(productId, imageId, size, cancellationToken);
        return image is null ? NotFound() : File(image.Data, image.ContentType, enableRangeProcessing: true);
    }

    [HttpDelete("{productId}/images/{imageId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteImage(string productId, string imageId, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.DeleteImageAsync(productId, imageId, cancellationToken));
    }

    private static async Task<IReadOnlyList<ProductImageRequest>> ReadImagesAsync(
        IEnumerable<IFormFile> files, CancellationToken cancellationToken)
    {
        var images = new List<ProductImageRequest>();
        foreach (var file in files)
        {
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            images.Add(new ProductImageRequest(stream.ToArray(), file.ContentType, file.FileName));
        }
        return images;
    }
}
