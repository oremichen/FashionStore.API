using FashionStore.API.Features.Orders.Shared;
using FashionStore.Domain.Abstractions.Contacts;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Abstractions.Notification;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Constants;
using System.Text.Encodings.Web;

namespace FashionStore.API.Features.Orders.UpdateOrderStatus;

public sealed class UpdateOrderStatusService(
    IOrderRepository orderRepository,
    IEmailNotificationService emailService,
    IEmailTemplateRenderer templateRenderer,
    IContactUsConfigurationRepository contactRepository,
    IConfiguration configuration,
    ILogger<UpdateOrderStatusService> logger,
    IDeliveryMethodClassifier deliveryClassifier,
    IDeliveryMethodFactory deliveryFactory) : IUpdateOrderStatusService
{
    public async Task<ResponseResult<OrderResponse>> ExecuteAsync(string id, UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetOrderByIdWithDetailsAsync(id, trackChanges: true, cancellationToken);
        if (order is null)
            return new ResponseResult<OrderResponse>().Fail("Order was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        try
        {
            order.UpdateStatus(request.Status, request.Message ?? request.RefundedReason);
        }
        catch (ArgumentException exception)
        {
            logger.LogWarning(exception, "Order status update validation failed for {OrderId}.", id);
            return new ResponseResult<OrderResponse>().Fail("The order status update is invalid.", ResponseCodes.INVALID_ACTION);
        }

        await orderRepository.SaveChangesAsync(cancellationToken);
        var address = await orderRepository.GetAddressByIdAsync(order.AddressId, cancellationToken);
        if (request.Status.Equals(Domain.Constants.OrderStatuses.Shipped, StringComparison.OrdinalIgnoreCase) ||
            request.Status.Equals(Domain.Constants.OrderStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
            request.Status.Equals(Domain.Constants.OrderStatuses.Cancelled, StringComparison.OrdinalIgnoreCase) ||
            request.Status.Equals(Domain.Constants.OrderStatuses.Returned, StringComparison.OrdinalIgnoreCase))
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
        var productImages = await orderRepository.GetPrimaryProductImagesAsync(productIds, cancellationToken);

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

        var strategy = deliveryFactory.GetForMethod(order.DeliveryMethod);
        var status = strategy.GetStatusLabel(order.Status);

        var deliveryAddress = strategy.ShowAddressInEmails
            ? deliveryClassifier.FormatDeliveryAddress(address)
            : HtmlEncoder.Default.Encode(string.Empty);

        var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appName"] = HtmlEncoder.Default.Encode(appName),
            ["customerName"] = HtmlEncoder.Default.Encode(order.User?.FirstName ?? order.Email),
            ["trackOrderId"] = HtmlEncoder.Default.Encode(order.TrackOrderId),
            ["status"] = HtmlEncoder.Default.Encode(status),
            ["statusMessage"] = HtmlEncoder.Default.Encode(statusMessage ?? strategy.GetCustomerStatusMessage(order.Status)),
            ["deliveryAddress"] = deliveryAddress,
            ["phoneNumber"] = HtmlEncoder.Default.Encode(address?.PhoneNumber ?? order.User?.PhoneNumber ?? "Not provided"),
            ["orderItemsHtml"] = BuildOrderItemsHtml(order.Items),
            ["year"] = DateTime.UtcNow.Year.ToString()
        };
        var customerBody = await templateRenderer.RenderAsync(Domain.Enums.EmailNotificationTypeEnum.OrderStatusUpdate, tokens);
        var adminTokens = new Dictionary<string, string>(tokens, StringComparer.OrdinalIgnoreCase)
        {
            ["statusMessage"] = HtmlEncoder.Default.Encode(statusMessage ?? strategy.GetAdminStatusMessage(order.Status))
        };
        var adminBody = await templateRenderer.RenderAsync(Domain.Enums.EmailNotificationTypeEnum.OrderStatusInternalUpdate, adminTokens);
        var customerSubject = $"Order {status} – {order.TrackOrderId}";
        var adminSubject = $"Order update: {order.TrackOrderId} – {status}";
        await emailService.QueueEmailAsync(new Domain.Entities.EmailNotification { To = [order.Email], Subject = customerSubject, Body = customerBody }, cancellationToken);
        if (recipients.Count > 0)
            await emailService.QueueEmailAsync(new Domain.Entities.EmailNotification { To = recipients, ReplyTo = order.Email, Subject = adminSubject, Body = adminBody }, cancellationToken);
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
}
