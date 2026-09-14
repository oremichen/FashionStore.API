using FashionStore.Domain.Entities;

namespace FashionStore.Domain.Abstractions.Orders;

public interface IOrderItemHtmlRendererService
{
    string Render(IEnumerable<OrderItem> items, bool includeSku);
}
