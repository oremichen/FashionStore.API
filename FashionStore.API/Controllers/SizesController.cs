namespace FashionStore.API.Controllers;
[Route("api/sizes")]
[ApiController]
public sealed class SizesController(FashionStore.Application.Abstractions.CatalogOptions.ICatalogOptionService service) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetSizesAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSizeRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.CreateSizeAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.DeleteSizeAsync(id, cancellationToken));
    }
}
