using System.Globalization;
using System.Text.Encodings.Web;
using FashionStore.API.Features.Payments.Shared;
using FashionStore.Domain.Abstractions.Contacts;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Abstractions.Notification;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Constants;
using FashionStore.Domain.Entities;

namespace FashionStore.API.Features.Payments.VerifyPaystack;

public sealed class VerifyPaystackService : IVerifyPaystackService
{
    private static readonly TimeSpan ReservationVerificationGrace = TimeSpan.FromSeconds(5);

    private readonly IOrderRepository _orderRepository;
    private readonly IEmailNotificationService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IContactUsConfigurationRepository _contactConfigRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerifyPaystackService> _logger;
    private readonly IDeliveryMethodClassifier _deliveryClassifier;
    private readonly IDeliveryMethodFactory _deliveryFactory;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;

    public VerifyPaystackService(
        IOrderRepository orderRepository,
        IEmailNotificationService emailService,
        IEmailTemplateRenderer templateRenderer,
        IContactUsConfigurationRepository contactConfigRepository,
        IConfiguration configuration,
        ILogger<VerifyPaystackService> logger,
        IDeliveryMethodClassifier deliveryClassifier,
        IDeliveryMethodFactory deliveryFactory,
        IPaymentGatewayFactory paymentGatewayFactory)
    {
        _orderRepository = orderRepository;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _contactConfigRepository = contactConfigRepository;
        _configuration = configuration;
        _logger = logger;
        _deliveryClassifier = deliveryClassifier;
        _deliveryFactory = deliveryFactory;
        _paymentGatewayFactory = paymentGatewayFactory;
    }

    public async Task<ResponseResult<PaymentVerificationResponse>> ExecuteAsync(string reference, string? userId, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PaymentVerificationResponse>();
        if (string.IsNullOrWhiteSpace(reference))
            return response.Fail("Payment reference is required.", ResponseCodes.INVALID_REFERENCE_PROVIDED);

        var details = await _orderRepository.GetByPaymentReferenceWithDetailsAsync(reference, true, cancellationToken);
        var order = details.Order;
        var address = details.Address;
        var user = details.User;

        if (order is null || (userId is not null && order.UserId != userId))
        {
            _logger.LogWarning("Payment verification could not locate reference {Reference} for user {UserId}.", reference, userId);
            return response.Fail("Payment reference was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }
        if (order.PaymentStatus == Domain.Constants.PaymentStatuses.Success)
            return response.Success(new PaymentVerificationResponse(reference, order.Id, Domain.Constants.PaymentStatuses.Success), "Payment already verified.");

        var cutoff = DateTimeOffset.UtcNow.Add(ReservationVerificationGrace);
        var expiringReservation = order.InventoryReservations.FirstOrDefault(item =>
            item.Status == Domain.Constants.InventoryReservationStatuses.Reserved && item.ExpiresAt <= cutoff);
        if (expiringReservation is not null)
        {
            _logger.LogWarning(
                "Payment verification skipped for order {OrderId} (reference {Reference}): reservation {ReservationId} " +
                "expires at {ExpiresAt} which is within the {Grace} grace window of now. " +
                "Avoiding a race with the expiry worker; caller should start a fresh checkout.",
                order.Id, reference, expiringReservation.Id, expiringReservation.ExpiresAt, ReservationVerificationGrace);
            return response.Fail("This checkout has expired or is about to expire. Please start a new order.", ResponseCodes.INVALID_ACTION);
        }

        Domain.Abstractions.Payments.PaymentVerificationResult transaction;
        try
        {
            var gateway = _paymentGatewayFactory.Get(PaymentProviderKeys.Paystack);
            transaction = await gateway.VerifyByMerchantReferenceAsync(reference, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or OverflowException)
        {
            _logger.LogError(exception, "Paystack verify API call failed for order {OrderId} and reference {Reference}.", order.Id, reference);
            return response.Fail("Payment verification is temporarily unavailable.", ResponseCodes.SERVICE_UNAVAILABLE);
        }

        var expectedAmount = checked(decimal.ToInt64(order.Total * 100m));
        var detailsMatch = transaction.ProviderReference == order.PaymentReference &&
            transaction.AmountKobo == expectedAmount && string.Equals(transaction.Currency, order.Currency, StringComparison.OrdinalIgnoreCase);
        if (!detailsMatch)
        {
            _logger.LogError("Paystack verification mismatch for order {OrderId}. Expected {Amount} {Currency}; received {PaidAmount} {PaidCurrency}.",
                order.Id, expectedAmount, order.Currency, transaction.AmountKobo, transaction.Currency);
            return response.Fail("Payment details did not match the order.", ResponseCodes.SECURITY_VIOLATION);
        }

        var paymentSucceeded = transaction.IsSuccess;
        var paidAt = transaction.PaidAt ?? DateTimeOffset.UtcNow;
        var failedStatus = paymentSucceeded
            ? Domain.Constants.PaymentStatuses.Failed
            : (string.IsNullOrWhiteSpace(transaction.RawStatus)
                ? Domain.Constants.PaymentStatuses.Failed
                : transaction.RawStatus);

        var (commitSucceeded, conflictMessage) = await _orderRepository.ExecuteInRetriableTransactionAsync(async (orderTransaction, ct) =>
        {
            string? reservationConflictMessage = null;
            if (paymentSucceeded)
            {
                order.MarkPaid(paidAt);
                foreach (var reservation in order.InventoryReservations.Where(item => item.Status == Domain.Constants.InventoryReservationStatuses.Reserved))
                {
                    var consumed = await _orderRepository.ConsumeInventoryReservationAsync(reservation.Id, ct);
                    if (!consumed)
                    {
                        reservationConflictMessage =
                            $"Inventory reservation {reservation.Id} for order {order.Id} could not be consumed. " +
                            "It was likely released or expired concurrently.";
                        break;
                    }
                }
            }
            else
            {
                order.MarkPaymentFailed(failedStatus);
                foreach (var reservation in order.InventoryReservations.Where(item => item.Status == Domain.Constants.InventoryReservationStatuses.Reserved))
                {
                    var released = await _orderRepository.ReleaseInventoryReservationAsync(reservation.Id, Domain.Constants.InventoryReservationStatuses.Released, ct);
                    if (!released)
                    {
                        reservationConflictMessage =
                            $"Inventory reservation {reservation.Id} for order {order.Id} could not be released. " +
                            "It was likely consumed or expired concurrently.";
                        break;
                    }
                }
            }

            if (reservationConflictMessage is not null)
            {
                _logger.LogError("{Message} Rolling back order state change and returning an error.", reservationConflictMessage);
                await orderTransaction.RollbackAsync(ct);
                return (Success: false, ConflictMessage: reservationConflictMessage);
            }

            await _orderRepository.SaveChangesAsync(ct);
            await orderTransaction.CommitAsync(ct);
            return (Success: true, ConflictMessage: (string?)null);
        }, cancellationToken);

        if (!commitSucceeded)
        {
            return response.Fail(
                "Your payment could not be finalized because the reserved inventory expired or changed just as we were confirming it. " +
                "Please start a new checkout.", ResponseCodes.INVALID_ACTION);
        }
        _ = conflictMessage;

        if (paymentSucceeded)
        {
            try
            {
                await SendOrderEmailsAsync(order, address, user, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order emails for order {OrderId} ({TrackOrderId}).", order.Id, order.TrackOrderId);
            }
        }

        _logger.LogInformation("Payment reference {Reference} for order {OrderId} ({TrackOrderId}) verified with status {Status}.",
            reference, order.Id, order.TrackOrderId, order.PaymentStatus);
        return response.Success(new PaymentVerificationResponse(reference, order.Id, order.PaymentStatus), "Payment verification completed.");
    }

    private async Task SendOrderEmailsAsync(Order order, Address? address, ApplicationUser? user, CancellationToken cancellationToken)
    {
        var appName = _configuration["AppSettings:AppName"] ?? "MaisonDeLola";
        var websiteUrl = _configuration["AppSettings:WebsiteUrl"] ?? "themaisondelola.com";
        var contactConfig = await _contactConfigRepository.GetActiveAsync(cancellationToken);
        var contactEmail = contactConfig?.ContactEmail ?? _configuration["EmailTemplates:DefaultContactEmail"] ?? "support@themaisondelola.com";
        var businessEmail = contactConfig?.BusinessEmail;

        var customerName = !string.IsNullOrWhiteSpace(user?.FirstName)
            ? $"{HtmlEncoder.Default.Encode(user.FirstName)} {HtmlEncoder.Default.Encode(user.LastName ?? string.Empty)}".Trim()
            : HtmlEncoder.Default.Encode(order.Email);

        var recipientName = customerName;
        var phoneNumber = HtmlEncoder.Default.Encode(address?.PhoneNumber ?? user?.PhoneNumber ?? "Not provided");
        var orderDate = order.CreatedAt.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var itemsTotal = _deliveryClassifier.FormatNaira(order.Subtotal);
        var deliveryFee = _deliveryClassifier.FormatNaira(order.DeliveryFee);
        var orderTotal = _deliveryClassifier.FormatNaira(order.Total);
        var customerOrderItemsHtml = BuildOrderItemsHtml(order.Items, includeSku: false);
        var internalOrderItemsHtml = BuildOrderItemsHtml(order.Items, includeSku: true);
        var totalQuantity = order.Items.Sum(i => i.Quantity);

        var strategy = _deliveryFactory.GetForMethod(order.DeliveryMethod);
        var deliveryMethod = strategy.GetDeliveryMethodDisplay(order.DeliveryMethod, order.EstimatedDaysMin, order.EstimatedDaysMax);
        var deliveryWindow = strategy.GetDefaultDeliveryWindowDisplay(order.EstimatedDaysMin, order.EstimatedDaysMax);
        var fullAddress = strategy.ShowAddressInEmails
            ? _deliveryClassifier.FormatDeliveryAddress(address)
            : HtmlEncoder.Default.Encode(string.Empty);

        var customerTemplate = strategy.GetOrderConfirmationTemplate(isInternal: false);
        var customerTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appName"] = HtmlEncoder.Default.Encode(appName),
            ["websiteUrl"] = HtmlEncoder.Default.Encode(websiteUrl),
            ["year"] = DateTime.UtcNow.Year.ToString(),
            ["customerName"] = customerName,
            ["trackOrderId"] = HtmlEncoder.Default.Encode(order.TrackOrderId),
            ["orderDate"] = HtmlEncoder.Default.Encode(orderDate),
            ["orderItemsHtml"] = customerOrderItemsHtml,
            ["itemsTotal"] = itemsTotal,
            ["deliveryFee"] = deliveryFee,
            ["deliveryMethod"] = deliveryMethod,
            ["deliveryWindow"] = deliveryWindow,
            ["orderTotal"] = orderTotal,
            ["recipientName"] = recipientName,
            ["deliveryAddress"] = fullAddress,
            ["phoneNumber"] = phoneNumber,
            ["contactEmail"] = HtmlEncoder.Default.Encode(contactEmail),
        };

        var customerBody = await _templateRenderer.RenderAsync(customerTemplate, customerTokens);
        await _emailService.QueueEmailAsync(new EmailNotification
        {
            To = [order.Email],
            Subject = $"Order Confirmed – {order.TrackOrderId} – {appName}",
            Body = customerBody
        }, cancellationToken);
        _logger.LogInformation("Customer order confirmation email queued for order {OrderId} ({TrackOrderId}).", order.Id, order.TrackOrderId);

        var internalRecipients = new List<string> { contactEmail };
        if (!string.IsNullOrWhiteSpace(businessEmail) &&
            !string.Equals(businessEmail, contactEmail, StringComparison.OrdinalIgnoreCase))
        {
            internalRecipients.Add(businessEmail);
        }

        var internalTemplate = strategy.GetOrderConfirmationTemplate(isInternal: true);
        var internalTokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["appName"] = HtmlEncoder.Default.Encode(appName),
            ["websiteUrl"] = HtmlEncoder.Default.Encode(websiteUrl),
            ["year"] = DateTime.UtcNow.Year.ToString(),
            ["customerName"] = customerName,
            ["customerEmail"] = HtmlEncoder.Default.Encode(order.Email),
            ["phoneNumber"] = phoneNumber,
            ["trackOrderId"] = HtmlEncoder.Default.Encode(order.TrackOrderId),
            ["orderDate"] = HtmlEncoder.Default.Encode(orderDate),
            ["paymentReference"] = HtmlEncoder.Default.Encode(order.PaymentReference),
            ["deliveryMethod"] = deliveryMethod,
            ["deliveryWindow"] = deliveryWindow,
            ["recipientName"] = recipientName,
            ["deliveryAddress"] = fullAddress,
            ["orderItemsHtml"] = internalOrderItemsHtml,
            ["totalQuantity"] = totalQuantity.ToString(),
            ["totalQuantityPlural"] = totalQuantity == 1 ? string.Empty : "s",
            ["itemsTotal"] = itemsTotal,
            ["deliveryFee"] = deliveryFee,
            ["orderTotal"] = orderTotal,
            ["customerNote"] = "No additional note was provided by the customer.",
        };

        var internalBody = await _templateRenderer.RenderAsync(internalTemplate, internalTokens);
        await _emailService.QueueEmailAsync(new EmailNotification
        {
            To = internalRecipients,
            ReplyTo = order.Email,
            Subject = $"New Order Received – {order.TrackOrderId} – {customerName}",
            Body = internalBody
        }, cancellationToken);
        _logger.LogInformation("Internal sales notification email queued for order {OrderId} ({TrackOrderId}) to {Recipients}.",
            order.Id, order.TrackOrderId, string.Join(", ", internalRecipients));
    }

    private string BuildOrderItemsHtml(IEnumerable<OrderItem> items, bool includeSku)
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
