using System.ComponentModel.DataAnnotations;
using FashionStore.Application.Abstractions.Brands;

namespace FashionStore.API.Controllers;

[Route("api/brands")]
[ApiController]
public sealed class BrandsController(IBrandService brandService) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await brandService.GetAllAsync(cancellationToken);
        return ProcessResponse(response);
    }

    [AllowAnonymous]
    [HttpGet("{id}/image")]
    [Produces("image/jpeg", "image/png", "image/webp", "image/gif")]
    public async Task<IActionResult> GetImage(string id, CancellationToken cancellationToken)
    {
        var image = await brandService.GetImageAsync(id, cancellationToken);
        return image is null ? NotFound() : File(image.Data, image.ContentType, enableRangeProcessing: true);
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
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
