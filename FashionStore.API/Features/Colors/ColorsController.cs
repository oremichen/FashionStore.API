namespace FashionStore.API.Features.Colors;
using FashionStore.API.Features.Colors.CreateColor;
using FashionStore.API.Features.Colors.DeleteColor;
using FashionStore.API.Features.Colors.GetColors;
[Route("api/colors")]
[ApiController]
public sealed class ColorsController(IGetColorsService getColorsService, ICreateColorService createColorService, IDeleteColorService deleteColorService) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getColorsService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateColorRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createColorService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteColorService.ExecuteAsync(id, cancellationToken));
    }
}
