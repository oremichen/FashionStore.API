using System.ComponentModel.DataAnnotations;
using FashionStore.Application.Abstractions.Brands;
using FashionStore.Application.Dtos.Response;

namespace FashionStore.API.Controllers;

[Route("api/brands")]
[ApiController]
public sealed class BrandsController(IBrandService brandService) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<BrandResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await brandService.GetAllAsync(cancellationToken);
        return ProcessResponse(response);
    }

    [AllowAnonymous]
    [HttpGet("{id}/image")]
    [Produces("image/jpeg", "image/png", "image/webp", "image/gif")]
    [ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, "image/jpeg", "image/png", "image/webp", "image/gif")]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetImage(string id, CancellationToken cancellationToken)
    {
        var image = await brandService.GetImageAsync(id, cancellationToken);
        return image is null ? NotFound() : File(image.Data, image.ContentType, enableRangeProcessing: true);
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [Produces("application/json")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [ProducesResponseType(typeof(ResponseResult<BrandResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<BrandResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult<BrandResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromForm] CreateBrandForm form, CancellationToken cancellationToken)
    {
        byte[]? data = null;
        if (form.Image is not null)
        {
            await using var stream = new MemoryStream();
            await form.Image.CopyToAsync(stream, cancellationToken);
            data = stream.ToArray();
        }
        var request = new CreateBrandRequest(form.Name, form.Slug, form.Description, form.WebsiteUrl, form.IsActive,
            data, form.Image?.ContentType, form.Image?.FileName);
        return ProcessResponse(await brandService.CreateAsync(request, cancellationToken));
    }
}
