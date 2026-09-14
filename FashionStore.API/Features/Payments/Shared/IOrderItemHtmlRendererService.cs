using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Entities;

namespace FashionStore.API.Features.Payments.Shared;

public interface IOrderItemHtmlRendererService
{
    string Render(IEnumerable<OrderItem> items, bool includeSku);
}
