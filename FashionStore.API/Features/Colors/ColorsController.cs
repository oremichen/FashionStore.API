namespace FashionStore.API.Features.Colors;
using FashionStore.API.Features.Colors.CreateColor;
using FashionStore.API.Features.Colors.DeleteColor;
using FashionStore.API.Features.Colors.GetColors;
[Route("api/colors")]
[ApiController]
public sealed class ColorsController(IGetColorsService getColorsService, ICreateColorService createColorService, IDeleteColorService deleteColorService) : BaseApiController
{
    [AllowAnonymous, HttpGet]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<ColorResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        return ProcessResponse(await getColorsService.ExecuteAsync(page, pageSize, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin"), HttpPost]
    [ProducesResponseType(typeof(ResponseResult<ColorResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromBody] CreateColorRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createColorService.ExecuteAsync(request, cancellationToken));
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
        return ProcessResponse(await deleteColorService.ExecuteAsync(id, cancellationToken));
    }
}
