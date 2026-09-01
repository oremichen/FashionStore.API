namespace FashionStore.API.Features.Sizes;
using FashionStore.API.Features.Sizes.CreateSize;
using FashionStore.API.Features.Sizes.DeleteSize;
using FashionStore.API.Features.Sizes.GetSizes;
using FashionStore.API.Features.Sizes.UpdateSize;
[Route("api/sizes")]
[ApiController]
public sealed class SizesController(IGetSizesService getSizesService, ICreateSizeService createSizeService, IUpdateSizeService updateSizeService, IDeleteSizeService deleteSizeService) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<SizeResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getSizesService.ExecuteAsync(page, pageSize, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPut("{id}")]
    [ProducesResponseType(typeof(ResponseResult<SizeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateSizeRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await updateSizeService.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [ProducesResponseType(typeof(ResponseResult<SizeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateSizeRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createSizeService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpDelete("{id}")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteSizeService.ExecuteAsync(id, cancellationToken));
    }
}
