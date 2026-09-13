using FashionStore.API.Features.Delivery.CreateRate;
using FashionStore.API.Features.Delivery.DeleteRate;
using FashionStore.API.Features.Delivery.GetDeliveryMethods;
using FashionStore.API.Features.Delivery.GetRates;
using FashionStore.API.Features.Delivery.GetMethods;
using FashionStore.API.Features.Delivery.GetZones;
using FashionStore.API.Features.Delivery.Shared;
using FashionStore.API.Features.Delivery.UpdateRate;

namespace FashionStore.API.Features.Delivery;

[ApiController, Route("api/delivery")]
public sealed class DeliveryController(
    IGetDeliveryMethodsService getDeliveryMethodsService,
    IGetRatesService getRatesService,
    ICreateRateService createRateService,
    IUpdateRateService updateRateService,
    IGetZonesService getZonesService,
    IGetMethodsService getMethodsService,
    IDeleteRateService deleteRateService) : BaseApiController
{
    [HttpGet("methods")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<DeliveryMethodResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDeliveryMethods([FromQuery] GetDeliveryMethodsRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getDeliveryMethodsService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("~/api/admin/delivery/rates")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<DeliveryRateResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetRates([FromQuery] GetRatesRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getRatesService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("~/api/admin/delivery/zones")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<DeliveryZoneResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetZones(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getZonesService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("~/api/admin/delivery/methods")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<IReadOnlyList<DeliveryMethodCatalogResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMethods(CancellationToken cancellationToken)
    {
        return ProcessResponse(await getMethodsService.ExecuteAsync(cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPost("~/api/admin/delivery/rates")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<DeliveryRateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateRate([FromBody] CreateRateRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createRateService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPut("~/api/admin/delivery/rates/{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult<DeliveryRateResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateRate(string id, [FromBody] UpdateRateRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await updateRateService.ExecuteAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpDelete("~/api/admin/delivery/rates/{id}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteRate(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await deleteRateService.ExecuteAsync(id, cancellationToken));
    }
}
