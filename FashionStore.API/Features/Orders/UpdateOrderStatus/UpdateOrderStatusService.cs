using FashionStore.API.Features.Orders.Shared;
using FashionStore.Domain.Abstractions.Contacts;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Abstractions.Notification;
using FashionStore.Domain.Constants;
using FashionStore.Domain.Entities;
using FashionStore.Domain.Enums;
using FashionStore.Infrastructure.Data;
using FashionStore.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;

namespace FashionStore.API.Features.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusService(
    FashionStoreDbContext db,
    IEmailNotificationService emailService,
    IEmailTemplateRenderer templateRenderer,
    IContactUsConfigurationRepository contactRepository,
    IConfiguration configuration,
    ILogger<UpdateOrderStatusService> logger,
    IDeliveryMethodClassifier deliveryClassifier) : IUpdateOrderStatusService
{
    public async Task<ResponseResult<OrderResponse>> ExecuteAsync(string id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var order = await db.Orders.Include("User").Include("Items").SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (order is null)
            return new ResponseResult<OrderResponse>().Fail("Order was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        try
        {
            order.UpdateStatus(request.Status, request.Message ?? request.RefundedReason);
        }
        catch (ArgumentException exception)
        {
            return new ResponseResult<OrderResponse>().Fail(exception.Message, ResponseCodes.INVALID_ACTION);
        }

        await db.SaveChangesAsync(cancellationToken);
        var address = await db.Addresses.AsNoTracking().SingleOrDefaultAsync(item => item.Id == order.AddressId, cancellationToken);
        if (request.Status.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase) ||
            request.Status.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
            request.Status.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
            request.Status.Equals(OrderStatuses.Returned, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                await SendStatusEmailsAsync(order, address, request.Message ?? request.RefundedReason, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to queue status emails for order {TrackOrderId}.", order.TrackOrderId);
            }
        }
        var productIds = order.Items
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();
        var productImages = await (from product in db.Products
                                   from image in product.Images
                                   where productIds.Contains(product.Id) && image.IsPrimary
                                   select new { product.Id, image.SmallUrl, image.MediumUrl, image.BigUrl })
            .ToDictionaryAsync(item => item.Id, item => item.SmallUrl ?? item.MediumUrl ?? item.BigUrl, cancellationToken);

        return new ResponseResult<OrderResponse>()
            .Success(OrderResponseMapper.Map(
            order,
            address is null ? null : UserAddressResponse.From(address),
            productImages),
            "Order status updated successfully.");
    }

    private async Task SendStatusEmailsAsync(Order order, Address? address, string? statusMessage, CancellationToken cancellationToken)
    {
        var appName = configuration["AppSettings:AppName"] ?? "MaisonDeLolaStore";
        var contact = await contactRepository.GetActiveAsync(cancellationToken);
        var contactEmail = contact?.ContactEmail ?? configuration["EmailTemplates:DefaultContactEmail"];
        var recipients = new List<string>();
        AddRecipient(recipients, contactEmail);
        AddRecipient(recipients, contact?.BusinessEmail);

        var isPickup = deliveryClassifier.IsPickup(order.DeliveryMethod);
        var status = GetStatusLabel(order.Status, isPickup);

        var deliveryAddress = isPickup
            ? HtmlEncoder.Default.Encode(string.Empty)
            : deliveryClassifier.FormatDeliveryAddress(address);

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appName"] = HtmlEncoder.Default.Encode(appName),
            ["customerName"] = HtmlEncoder.Default.Encode(order.User?.FirstName ?? order.Email),
            ["trackOrderId"] = HtmlEncoder.Default.Encode(order.TrackOrderId),
            ["status"] = HtmlEncoder.Default.Encode(status),
            ["statusMessage"] = HtmlEncoder.Default.Encode(statusMessage ?? GetDefaultMessage(order.Status, isPickup)),
            ["deliveryAddress"] = deliveryAddress,
            ["phoneNumber"] = HtmlEncoder.Default.Encode(address?.PhoneNumber ?? order.User?.PhoneNumber ?? "Not provided"),
            ["orderItemsHtml"] = BuildOrderItemsHtml(order.Items),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };
        var customerBody = await templateRenderer.RenderAsync(EmailNotificationTypeEnum.OrderStatusUpdate, tokens);
        var adminTokens = new Dictionary<string, string>(tokens, StringComparer.OrdinalIgnoreCase)
        {
            ["statusMessage"] = HtmlEncoder.Default.Encode(statusMessage ?? GetAdminMessage(order.Status, isPickup))
        };
        var adminBody = await templateRenderer.RenderAsync(EmailNotificationTypeEnum.OrderStatusInternalUpdate, adminTokens);
        var customerSubject = $"Order {status} – {order.TrackOrderId}";
        var adminSubject = $"Order update: {order.TrackOrderId} – {status}";
        await emailService.QueueEmailAsync(new EmailNotification { To = [order.Email], Subject = customerSubject, Body = customerBody }, cancellationToken);
        if (recipients.Count > 0)
            await emailService.QueueEmailAsync(new EmailNotification { To = recipients, ReplyTo = order.Email, Subject = adminSubject, Body = adminBody }, cancellationToken);
    }

    private static string BuildOrderItemsHtml(IEnumerable<OrderItem> items)
    {
        var rows = new List<string>();
        foreach (var item in items)
            rows.Add($"<tr><td style=\"padding:10px 12px;border-bottom:1px solid #eee;\">{HtmlEncoder.Default.Encode(item.ProductName)}<br /><span style=\"font-size:12px;color:#777;\">{HtmlEncoder.Default.Encode(BuildVariant(item))}</span></td><td style=\"padding:10px 12px;border-bottom:1px solid #eee;text-align:center;\">{item.Quantity}</td><td style=\"padding:10px 12px;border-bottom:1px solid #eee;text-align:right;\">{item.LineTotal:N2}</td></tr>");
        return $"<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\"><tr><th align=\"left\" style=\"padding:10px 12px;background:#fafafa;\">Item</th><th style=\"padding:10px 12px;background:#fafafa;\">Qty</th><th align=\"right\" style=\"padding:10px 12px;background:#fafafa;\">Total</th></tr>{string.Join(string.Empty, rows)}</table>";
    }

    private static string BuildVariant(OrderItem item)
    {
        var details = new List<string>();
        AddPart(details, item.ColorName);
        AddPart(details, item.SizeName);
        return string.Join(" / ", details);
    }

    private static void AddPart(List<string> parts, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            parts.Add(value);
    }

    private static void AddRecipient(List<string> recipients, string? email)
    {
        if (!string.IsNullOrWhiteSpace(email) && !recipients.Contains(email, StringComparer.OrdinalIgnoreCase))
            recipients.Add(email);
    }

    private static string GetStatusLabel(string status, bool isPickup)
    {
        if (status.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
            return isPickup ? "Ready for Pickup" : "Shipping";
        if (status.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            return isPickup ? "Picked Up" : "Delivered";
        if (status.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)) return "Cancelled";
        return "Returned";
    }

    private static string GetDefaultMessage(string status, bool isPickup)
    {
        if (status.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
            return isPickup
                ? "Your order is ready for pickup. Please visit our store during business hours to collect your items."
                : "Your order has been shipped and is on its way to you.";
        if (status.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            return isPickup
                ? "Your order has been picked up. Thank you for shopping with us and we hope you enjoy your purchase."
                : "Your order has been delivered. We hope you enjoy your purchase.";
        if (status.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)) return "Your order has been cancelled. If a refund applies, it will be processed according to our policy.";
        return "Your returned order has been received and is being reviewed.";
    }

    private static string GetAdminMessage(string status, bool isPickup)
    {
        if (status.Equals(OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase))
            return isPickup
                ? "Action recorded: the pickup order has been marked as ready for collection. Ensure items are packaged and the customer has been notified to visit the store."
                : "Action recorded: the order has been marked as shipped. Confirm courier handover and tracking details are available to the customer.";
        if (status.Equals(OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
            return isPickup
                ? "Action recorded: the pickup order has been marked as collected. Confirm customer identification was verified and the items were handed over successfully."
                : "Action recorded: the order has been marked as delivered. Confirm delivery completion and resolve any outstanding fulfilment tasks.";
        if (status.Equals(OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)) return "Action recorded: the order has been cancelled. Review the cancellation reason and process any applicable refund or stock adjustment.";
        return "Action recorded: the order has been marked as returned. Inspect the returned items and update the refund or restocking outcome.";
    }
}
