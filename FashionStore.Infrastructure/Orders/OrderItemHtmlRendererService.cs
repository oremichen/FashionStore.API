using System.Text.Encodings.Web;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Entities;

namespace FashionStore.Infrastructure.Orders;

public sealed class OrderItemHtmlRendererService : IOrderItemHtmlRendererService
{
    private readonly IDeliveryMethodClassifier _deliveryClassifier;

    public OrderItemHtmlRendererService(IDeliveryMethodClassifier deliveryClassifier)
    {
        _deliveryClassifier = deliveryClassifier;
    }

    public string Render(IEnumerable<OrderItem> items, bool includeSku)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var item in items)
        {
            var unitPrice = _deliveryClassifier.FormatNaira(item.UnitPrice);
            var lineTotal = _deliveryClassifier.FormatNaira(item.LineTotal);
            sb.Append("<div style=\"padding:16px 20px; border-bottom:1px solid #f0f0f0;\">");
            sb.Append("<div style=\"font-size:15px; font-weight:700; color:#212121; margin-bottom:8px;\">");
            sb.Append(HtmlEncoder.Default.Encode(item.ProductName));
            sb.Append("</div>");

            if (includeSku && !string.IsNullOrWhiteSpace(item.ProductId))
            {
                sb.Append("<div style=\"font-size:12px; color:#757575; margin-bottom:6px;\">");
                sb.Append("<strong style=\"color:#424242;\">Product ID/SKU:</strong> ");
                sb.Append(HtmlEncoder.Default.Encode(item.ProductId));
                sb.Append("</div>");
            }

            if (!string.IsNullOrWhiteSpace(item.ColorName))
            {
                sb.Append("<div style=\"font-size:13px; color:#424242; margin-bottom:6px;\">");
                sb.Append("<strong style=\"color:#616161;\">Color:</strong> ");
                sb.Append(HtmlEncoder.Default.Encode(item.ColorName));
                sb.Append("</div>");
            }

            if (!string.IsNullOrWhiteSpace(item.SizeName))
            {
                var sizeLabel = item.SizeName.Contains("Yard", StringComparison.OrdinalIgnoreCase)
                    || item.SizeName.Contains("meter", StringComparison.OrdinalIgnoreCase)
                    ? "Size/Length" : "Size";
                sb.Append("<div style=\"font-size:13px; color:#424242; margin-bottom:6px;\">");
                sb.Append("<strong style=\"color:#616161;\">");
                sb.Append(sizeLabel);
                sb.Append(":</strong> ");
                sb.Append(HtmlEncoder.Default.Encode(item.SizeName));
                sb.Append("</div>");
            }

            sb.Append("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"margin-top:6px;\">");
            sb.Append("<tr>");
            sb.Append("<td style=\"padding:3px 0; font-size:13px; color:#424242; width:33%;\"><strong style=\"color:#616161;\">Quantity:</strong> ");
            sb.Append(HtmlEncoder.Default.Encode(item.Quantity.ToString()));
            sb.Append("</td>");
            sb.Append("<td style=\"padding:3px 0; font-size:13px; color:#424242; width:33%;\"><strong style=\"color:#616161;\">Unit Price:</strong> ");
            sb.Append(unitPrice);
            sb.Append("</td>");
            sb.Append("<td style=\"padding:3px 0; font-size:13px; color:#424242; text-align:right; width:34%;\"><strong style=\"color:#6b4f12;\">Subtotal:</strong> ");
            sb.Append(lineTotal);
            sb.Append("</td>");
            sb.Append("</tr>");
            sb.Append("</table>");
            sb.Append("</div>");
        }
        return sb.ToString();
    }
}
