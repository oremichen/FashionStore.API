namespace FashionStore.API.Controllers;
[Route("api/colors")]
[ApiController]
public sealed class ColorsController(FashionStore.Application.Abstractions.CatalogOptions.ICatalogOptionService service) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.GetColorsAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateColorRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.CreateColorAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await service.DeleteColorAsync(id, cancellationToken));
    }
}
