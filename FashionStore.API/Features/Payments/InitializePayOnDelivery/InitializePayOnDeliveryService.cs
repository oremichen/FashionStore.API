namespace FashionStore.API.Features.Payments.InitializePayOnDelivery;

public sealed class InitializePayOnDeliveryService : IInitializePayOnDeliveryService
{
    private const int PayOnDeliveryReservationExpiryDays = 30;

    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IConfiguration _configuration;
    private readonly FashionStoreDbContext _dbContext;
    private readonly ILogger<InitializePayOnDeliveryService> _logger;
    private readonly IDeliveryMethodClassifier _deliveryClassifier;
    private readonly IDeliveryMethodFactory _deliveryFactory;
    private readonly IEmailNotificationService _emailService;
    private readonly IEmailTemplateRenderer _templateRenderer;
    private readonly IContactUsConfigurationRepository _contactConfigRepository;
    private readonly IOrderItemHtmlRendererService _orderItemHtmlRenderer;

    public InitializePayOnDeliveryService(
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IConfiguration configuration,
        FashionStoreDbContext dbContext,
        IDeliveryRepository deliveryRepository,
        ILogger<InitializePayOnDeliveryService> logger,
        IDeliveryMethodClassifier deliveryClassifier,
        IDeliveryMethodFactory deliveryFactory,
        IEmailNotificationService emailService,
        IEmailTemplateRenderer templateRenderer,
        IContactUsConfigurationRepository contactConfigRepository,
        IOrderItemHtmlRendererService orderItemHtmlRenderer)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _configuration = configuration;
        _dbContext = dbContext;
        _deliveryRepository = deliveryRepository;
        _logger = logger;
        _deliveryClassifier = deliveryClassifier;
        _deliveryFactory = deliveryFactory;
        _emailService = emailService;
        _templateRenderer = templateRenderer;
        _contactConfigRepository = contactConfigRepository;
        _orderItemHtmlRenderer = orderItemHtmlRenderer;
    }

    public async Task<ResponseResult<PayOnDeliveryInitializationResponse>> ExecuteAsync(
        string userId,
        InitializePayOnDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PayOnDeliveryInitializationResponse>();
        _logger.LogInformation("PayOnDelivery checkout initialization started for user {UserId} with {ItemCount} items.", userId, request.Items.Count);

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.AddressId) ||
            request.Items.Count == 0)
        {
            return response.Fail(
                "Idempotency key, email, address and at least one order item are required.",
                ResponseCodes.INVALID_ACTION);
        }

        var idempotencyKey = request.IdempotencyKey.Trim();
        if (idempotencyKey.Length > 100)
        {
            _logger.LogError("Idempotency key exceeds 100 characters for user {UserId}.", userId);
            return response.Fail("Idempotency key cannot exceed 100 characters.", ResponseCodes.INVALID_ACTION);
        }

        var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(userId, idempotencyKey, false, cancellationToken);
        if (existingOrder is not null)
        {
            if (string.Equals(existingOrder.PaymentProvider, PaymentProviderKeys.PayOnDelivery, StringComparison.OrdinalIgnoreCase))
            {
                return response.Success(
                    new PayOnDeliveryInitializationResponse(
                        existingOrder.Id,
                        existingOrder.TrackOrderId,
                        existingOrder.PaymentReference,
                        existingOrder.PaymentStatus),
                    "Returning the existing Pay On Delivery order.");
            }
            return response.Fail(
                "This checkout is already being initialized with a different payment method. Please start a new order.",
                ResponseCodes.REQUEST_IN_PROGRESS);
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryId))
            return response.Fail("A delivery option is required.", ResponseCodes.INVALID_ACTION);

        var address = await _dbContext.Addresses.FindAsync([request.AddressId], cancellationToken);
        if (address is null)
            return response.Fail("The selected address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var deliveryResolution = await ResolveDeliveryInfoAsync(
            request.DeliveryId, request.AddressId, address.State, userId, cancellationToken);
        if (deliveryResolution is null)
            return response.Fail("The selected delivery option could not be resolved.", ResponseCodes.INVALID_ACTION);
        if (deliveryResolution.ErrorMessage is not null)
            return response.Fail(deliveryResolution.ErrorMessage, ResponseCodes.INVALID_ACTION);

        if (!deliveryResolution.IsPickup
            && !await _orderRepository.AddressBelongsToUserAsync(request.AddressId, userId, cancellationToken))
        {
            _logger.LogError("User {UserId} attempted checkout with unavailable address {AddressId}.", userId, request.AddressId);
            return response.Fail("The selected address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var orderItems = await BuildOrderItemsAsync(request.Items, userId, cancellationToken);
        if (orderItems is null)
            return response.Fail("One or more products are unavailable.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var subtotal = orderItems.Sum(item => item.LineTotal);
        var reference = "FS-COD-" + Guid.NewGuid().ToString("N").ToUpperInvariant();

        var order = Order.Create(
            userId,
            idempotencyKey,
            request.AddressId,
            request.Email,
            deliveryResolution.DeliveryMethodName,
            deliveryResolution.DeliveryRateId,
            deliveryResolution.EstimatedDaysMin,
            deliveryResolution.EstimatedDaysMax,
            subtotal,
            deliveryResolution.DeliveryFee,
            reference,
            orderItems,
            paymentProvider: PaymentProviderKeys.PayOnDelivery);

        var reservationExpiresAt = DateTimeOffset.UtcNow
            .AddDays(PayOnDeliveryReservationExpiryDays);

        try
        {
            await _orderRepository.CreateWithInventoryReservationsAsync(order, reservationExpiresAt, cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == "P0001")
        {
            _logger.LogError(exception, "Inventory reservation was rejected for PayOnDelivery checkout {IdempotencyKey}.", idempotencyKey);
            return response.Fail(exception.MessageText, ResponseCodes.INVALID_ACTION);
        }

        try
        {
            await SendOrderEmailsAsync(order, address, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send PayOnDelivery order confirmation emails for order {OrderId} ({TrackOrderId}).",
                order.Id, order.TrackOrderId);
        }

        _logger.LogInformation(
            "PayOnDelivery order {OrderId} ({TrackOrderId}) created with reference {Reference}, total {Total} NGN, reservation expires {ExpiresAt}.",
            order.Id, order.TrackOrderId, reference, order.Total, reservationExpiresAt);

        return response.Success(
            new PayOnDeliveryInitializationResponse(
                order.Id,
                order.TrackOrderId,
                reference,
                order.PaymentStatus),
            "Order placed successfully. Payment will be collected on delivery.");
    }

    private async Task SendOrderEmailsAsync(Order order, Address? address, CancellationToken cancellationToken)
    {
        var appName = _configuration["AppSettings:AppName"] ?? "MaisonDeLola";
        var websiteUrl = _configuration["AppSettings:WebsiteUrl"] ?? "themaisondelola.com";
        var contactConfig = await _contactConfigRepository.GetActiveAsync(cancellationToken);
        var contactEmail = contactConfig?.ContactEmail
            ?? _configuration["EmailTemplates:DefaultContactEmail"]
            ?? "support@themaisondelola.com";
        var businessEmail = contactConfig?.BusinessEmail;

        var customerName = !string.IsNullOrWhiteSpace(order.Email)
            ? HtmlEncoder.Default.Encode(order.Email)
            : "Customer";

        var recipientName = customerName;
        var phoneNumber = HtmlEncoder.Default.Encode(address?.PhoneNumber ?? "Not provided");
        var orderDate = order.CreatedAt.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var itemsTotal = _deliveryClassifier.FormatNaira(order.Subtotal);
        var deliveryFee = _deliveryClassifier.FormatNaira(order.DeliveryFee);
        var orderTotal = _deliveryClassifier.FormatNaira(order.Total);
        var customerOrderItemsHtml = _orderItemHtmlRenderer.Render(order.Items, includeSku: false);
        var internalOrderItemsHtml = _orderItemHtmlRenderer.Render(order.Items, includeSku: true);
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
            ["paymentMethod"] = "Pay On Delivery",
            ["paymentStatus"] = "Awaiting Payment (on delivery)"
        };

        var customerBody = await _templateRenderer.RenderAsync(customerTemplate, customerTokens);
        await _emailService.QueueEmailAsync(new EmailNotification
        {
            To = [order.Email],
            Subject = $"Your Order is Confirmed – {order.TrackOrderId} – Pay On Delivery",
            Body = customerBody
        }, cancellationToken);
        _logger.LogInformation("Customer PayOnDelivery confirmation email queued for order {OrderId} ({TrackOrderId}).",
            order.Id, order.TrackOrderId);

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
            ["paymentMethod"] = "Pay On Delivery",
            ["paymentStatus"] = "Awaiting Payment (on delivery)",
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
            ["customerNote"] = "Pay On Delivery order – payment pending."
        };

        var internalBody = await _templateRenderer.RenderAsync(internalTemplate, internalTokens);
        await _emailService.QueueEmailAsync(new EmailNotification
        {
            To = internalRecipients,
            ReplyTo = order.Email,
            Subject = $"New Pay On Delivery Order – {order.TrackOrderId} – {customerName}",
            Body = internalBody
        }, cancellationToken);
        _logger.LogInformation("Internal PayOnDelivery notification email queued for order {OrderId} ({TrackOrderId}) to {Recipients}.",
            order.Id, order.TrackOrderId, string.Join(", ", internalRecipients));
    }

    private async Task<DeliveryResolutionResult?> ResolveDeliveryInfoAsync(
        string deliveryId,
        string addressId,
        string state,
        string userId,
        CancellationToken cancellationToken)
    {
        var activeContacts = await _dbContext.ContactUsConfigurations.AsNoTracking()
            .ToListAsync(cancellationToken);
        var activeContact = activeContacts.SingleOrDefault(IsActiveContact);

        var pickupMethod = await _deliveryRepository.GetMethodByIdAsync(deliveryId, cancellationToken);
        var pickupStrategy = pickupMethod is null ? null : _deliveryFactory.GetForMethod(pickupMethod);
        var isPickupOrder = pickupStrategy is not null
            && pickupStrategy.RequiresStoreAddressValidation
            && activeContact?.AddressId == addressId;

        if (isPickupOrder)
        {
            return new DeliveryResolutionResult
            {
                IsPickup = true,
                DeliveryMethodName = pickupMethod!.Name,
                DeliveryRateId = pickupMethod.Id,
                EstimatedDaysMin = null,
                EstimatedDaysMax = null,
                DeliveryFee = 0m,
                ErrorMessage = null
            };
        }

        var requestedRate = await _deliveryRepository.GetRateByIdAsync(deliveryId, cancellationToken);
        var requestedMethod = requestedRate is null
            ? null
            : await _deliveryRepository.GetMethodByIdAsync(requestedRate.MethodId, cancellationToken);

        var requestedMethodStrategy = requestedMethod is null ? null : _deliveryFactory.GetForMethod(requestedMethod);
        var isPickupAddress = activeContact?.AddressId == addressId &&
            requestedMethodStrategy?.RequiresStoreAddressValidation == true;

        var deliveryRate = await _deliveryRepository.GetActiveRateForStateAsync(deliveryId, state, cancellationToken);
        if (deliveryRate is null)
        {
            return new DeliveryResolutionResult
            {
                IsPickup = false,
                ErrorMessage = "The selected delivery option is unavailable for this address."
            };
        }

        return new DeliveryResolutionResult
        {
            IsPickup = false,
            DeliveryMethodName = deliveryRate.Method.Name,
            DeliveryRateId = deliveryRate.Id,
            EstimatedDaysMin = deliveryRate.EstimatedDaysMin,
            EstimatedDaysMax = deliveryRate.EstimatedDaysMax,
            DeliveryFee = deliveryRate.PriceKobo / 100m,
            ErrorMessage = null
        };
    }

    private async Task<List<OrderItem>?> BuildOrderItemsAsync(
        IEnumerable<CheckoutItemRequest> requestedItems,
        string userId,
        CancellationToken cancellationToken)
    {
        var orderItems = new List<OrderItem>();
        foreach (var requestedItem in requestedItems)
        {
            if (requestedItem.Quantity <= 0)
                return null;

            var product = await _productRepository.GetByIdAsync(requestedItem.ProductId, false, cancellationToken);
            if (product is null || !product.IsActive || product.IsArchived)
                return null;

            string? colorName = null;
            string? sizeName = null;
            decimal unitPrice;

            if (!string.IsNullOrWhiteSpace(requestedItem.VariantId))
            {
                var variant = product.Variants.FirstOrDefault(item => item.Id == requestedItem.VariantId && item.IsActive);
                if (variant is null || product.AvailabilityCount < requestedItem.Quantity)
                    return null;
                unitPrice = variant.NewPrice;

                if (!string.IsNullOrWhiteSpace(variant.SizeId))
                {
                    var size = await _dbContext.Sizes.AsNoTracking()
                        .FirstOrDefaultAsync(s => s.Id == variant.SizeId, cancellationToken);
                    sizeName = size?.DisplayName ?? size?.Name;
                }

                if (!string.IsNullOrWhiteSpace(variant.ColorId))
                {
                    var color = await _dbContext.Colors.AsNoTracking()
                        .FirstOrDefaultAsync(c => c.Id == variant.ColorId, cancellationToken);
                    colorName = color?.Name;
                }
            }
            else
            {
                if (product.Variants.Count > 0 || product.AvailabilityCount < requestedItem.Quantity)
                    return null;
                unitPrice = product.NewPrice;
            }

            if (!string.IsNullOrWhiteSpace(requestedItem.ColorId))
            {
                var color = await _dbContext.Colors.AsNoTracking()
                    .FirstOrDefaultAsync(c => c.Id == requestedItem.ColorId, cancellationToken);
                if (color is null)
                {
                    _logger.LogWarning("User {UserId} selected invalid color {ColorId} for product {ProductId}.",
                        userId, requestedItem.ColorId, product.Id);
                    return null;
                }
                colorName = color.Name;
            }

            if (!string.Equals(product.CurrencyCode, "NGN", StringComparison.OrdinalIgnoreCase))
                return null;
            orderItems.Add(OrderItem.Create(
                product.Id,
                requestedItem.VariantId,
                requestedItem.ColorId,
                colorName,
                sizeName,
                product.Name,
                unitPrice,
                requestedItem.Quantity));
        }
        return orderItems;
    }

    private static bool IsActiveContact(ContactUsConfiguration contact)
    {
        return contact.IsActive;
    }
}
