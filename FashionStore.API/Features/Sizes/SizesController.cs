namespace FashionStore.API.Features.Sizes;
using FashionStore.API.Features.Sizes.CreateSize;
using FashionStore.API.Features.Sizes.DeleteSize;
using FashionStore.API.Features.Sizes.GetSizes;
[Route("api/sizes")]
[ApiController]
public sealed class SizesController(IGetSizesService getSizesService, ICreateSizeService createSizeService, IDeleteSizeService deleteSizeService) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getSizesService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSizeRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createSizeService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteSizeService.ExecuteAsync(id, cancellationToken));
    }
}
