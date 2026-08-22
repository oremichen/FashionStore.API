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
        var request = new CreateBrandRequest
        {
            Name = form.Name, Slug = form.Slug, Description = form.Description, WebsiteUrl = form.WebsiteUrl,
            IsActive = form.IsActive, ImageData = data, ImageContentType = form.Image?.ContentType,
            ImageFileName = form.Image?.FileName
        };
        return ProcessResponse(await brandService.CreateAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await brandService.DeleteAsync(id, cancellationToken));
    }
}
