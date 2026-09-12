using FashionStore.API.Features.Delivery.CreateRate;
using FashionStore.API.Features.Delivery.GetDeliveryMethods;
using FashionStore.API.Features.Delivery.GetRates;
using FashionStore.API.Features.Delivery.UpdateRate;

namespace FashionStore.API.Features.Delivery;

[ApiController, Route("api/delivery")]
public sealed class DeliveryController(
    IGetDeliveryMethodsService getDeliveryMethodsService,
    IGetRatesService getRatesService,
    ICreateRateService createRateService,
    IUpdateRateService updateRateService) : BaseApiController
{
    [HttpGet("methods")]
    public async Task<IActionResult> GetDeliveryMethods([FromQuery] GetDeliveryMethodsRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getDeliveryMethodsService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("~/api/admin/delivery/rates")]
    public async Task<IActionResult> GetRates([FromQuery] GetRatesRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getRatesService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPost("~/api/admin/delivery/rates")]
    public async Task<IActionResult> CreateRate([FromBody] CreateRateRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await createRateService.ExecuteAsync(request, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPut("~/api/admin/delivery/rates/{id}")]
    public async Task<IActionResult> UpdateRate(string id, [FromBody] UpdateRateRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await updateRateService.ExecuteAsync(id, request, cancellationToken));
    }
}
