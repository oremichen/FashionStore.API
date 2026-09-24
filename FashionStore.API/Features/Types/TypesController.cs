using FashionStore.API.Features.Types.CreateType;
using FashionStore.API.Features.Types.DeleteType;
using FashionStore.API.Features.Types.GetTypes;
using FashionStore.API.Features.Types.UpdateType;

namespace FashionStore.API.Features.Types;

[Route("api/types")]
[ApiController]
public sealed class TypesController(
    IGetTypesService getTypesService,
    ICreateTypeService createTypeService,
    IUpdateTypeService updateTypeService,
    IDeleteTypeService deleteTypeService,
    ILogger<TypesController> logger) : BaseApiController
{
    [AllowAnonymous]
    [HttpGet]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<PagedResponse<TypeResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Received request to retrieve types. Page: {Page}, PageSize: {PageSize}.", page, pageSize);
        return ProcessResponse(await getTypesService.ExecuteAsync(page, pageSize, cancellationToken));
    }

    [Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.BusinessAdmin}")]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTypeRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to create type with slug {Slug}.", request.Slug);
        return ProcessResponse(await createTypeService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.BusinessAdmin}")]
    [HttpPut("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult<TypeResponse>), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTypeRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to update type {TypeId}.", id);
        return ProcessResponse(await updateTypeService.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.BusinessAdmin}")]
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Received request to delete type {TypeId}.", id);
        return ProcessResponse(await deleteTypeService.ExecuteAsync(id, cancellationToken));
    }
}
