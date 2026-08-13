using FashionStore.Application.Abstractions.MainCarousels;

namespace FashionStore.API.Controllers;

[Route("api/main-carousels")]
[ApiController]
public sealed class MainCarouselsController(IMainCarouselService service) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var response = await service.GetAllAsync(cancellationToken);
        return ProcessResponse(response);
    }

    [AllowAnonymous, HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var response = await service.GetByIdAsync(id, cancellationToken);
        return ProcessResponse(response);
    }

    [AllowAnonymous, HttpGet("{id}/image")]
    [Produces("image/jpeg", "image/png", "image/webp")]
    public async Task<IActionResult> GetImage(string id, CancellationToken cancellationToken)
    {
        var image = await service.GetImageAsync(id, cancellationToken);
        return image is null ? NotFound() : File(image.Data, image.ContentType, enableRangeProcessing: true);
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateMainCarouselForm form, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(form.Image, cancellationToken);
        var request = new CreateMainCarouselRequest(form.Title, form.Subtitle, form.ButtonText, form.LinkUrl, form.SortOrder,
            form.IsActive, data!, form.Image.ContentType, form.Image.FileName, form.ImageWidth, form.ImageHeight);
        return ProcessResponse(await service.CreateAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    [Consumes("multipart/form-data"), RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Update(string id, [FromForm] UpdateMainCarouselForm form, CancellationToken cancellationToken)
    {
        var data = await ReadAsync(form.Image, cancellationToken);
        var request = new UpdateMainCarouselRequest(form.Title, form.Subtitle, form.ButtonText, form.LinkUrl, form.SortOrder,
            form.IsActive, data, form.Image?.ContentType, form.Image?.FileName, form.ImageWidth, form.ImageHeight);
        return ProcessResponse(await service.UpdateAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        var response = await service.DeleteAsync(id, cancellationToken);
        return ProcessResponse(response);
    }

    private static async Task<byte[]?> ReadAsync(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null) return null;
        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream, cancellationToken);
        return stream.ToArray();
    }
}
