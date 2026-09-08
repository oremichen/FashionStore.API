using System.Security.Claims;
using FashionStore.API.Controllers;
using FashionStore.API.Features.Orders.GetOrderById;
using FashionStore.API.Features.Orders.GetOrders;
using FashionStore.API.Features.Orders.Shared;
using FashionStore.API.Features.Orders.UpdateOrderStatus;

namespace FashionStore.API.Features.Orders;

[ApiController, Authorize, Route("api/orders")]
public sealed class OrdersController(
    IGetOrdersService getOrdersService,
    IGetOrderByIdService getOrderByIdService,
    IUpdateOrderStatusService updateOrderStatusService) : BaseApiController
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMine([FromQuery] OrderQuery query, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ProcessResponse(await getOrdersService.ExecuteAsync(userId, query, false, cancellationToken));
    }

    [HttpGet("me/{id}")]
    public async Task<IActionResult> GetMineById(string id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return ProcessResponse(await getOrderByIdService.ExecuteAsync(id, userId, false, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("~/api/admin/orders")]
    public async Task<IActionResult> GetAdmin([FromQuery] OrderQuery query, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getOrdersService.ExecuteAsync(null, query, true, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpGet("~/api/admin/orders/{id}")]
    public async Task<IActionResult> GetAdminById(string id, CancellationToken cancellationToken)
    {
        return ProcessResponse(await getOrderByIdService.ExecuteAsync(id, null, true, cancellationToken));
    }

    [Authorize(Roles = "SuperAdmin,BusinessAdmin")]
    [HttpPut("~/api/admin/orders/{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        return ProcessResponse(await updateOrderStatusService.ExecuteAsync(id, request, cancellationToken));
    }
}
