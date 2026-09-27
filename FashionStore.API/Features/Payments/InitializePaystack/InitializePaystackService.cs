using FashionStore.Domain.Abstractions.CatalogOptions;
using FashionStore.Domain.Abstractions.Carts;
using FashionStore.Domain.Abstractions.Contacts;

namespace FashionStore.API.Features.Payments.InitializePaystack;

public sealed class InitializePaystackService : IInitializePaystackService
{
    private const double DefaultReservationExpiryHours = 24.0;
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<InitializePaystackService> _logger;
    private readonly IDeliveryMethodClassifier _deliveryClassifier;
    private readonly IDeliveryMethodFactory _deliveryFactory;
    private readonly IPaymentGatewayFactory _paymentGatewayFactory;
    private readonly IContactUsConfigurationRepository _contactConfigRepository;
    private readonly ICartRepository _cartRepository;
    private readonly ICheckoutOrderItemService _checkoutOrderItemService;
    private readonly TimeSpan _reservationLifetime;

    public InitializePaystackService(IOrderRepository orderRepository,
        IConfiguration configuration, IDeliveryRepository deliveryRepository,
        ILogger<InitializePaystackService> logger, IDeliveryMethodClassifier deliveryClassifier, IDeliveryMethodFactory deliveryFactory,
        IPaymentGatewayFactory paymentGatewayFactory,
        IContactUsConfigurationRepository contactConfigRepository,
        ICartRepository cartRepository,
        ICheckoutOrderItemService checkoutOrderItemService)
    {
        _orderRepository = orderRepository;
        _deliveryRepository = deliveryRepository;
        _configuration = configuration;
        _logger = logger;
        _deliveryClassifier = deliveryClassifier;
        _deliveryFactory = deliveryFactory;
        _paymentGatewayFactory = paymentGatewayFactory;
        _contactConfigRepository = contactConfigRepository;
        _cartRepository = cartRepository;
        _checkoutOrderItemService = checkoutOrderItemService;

        if (!double.TryParse(_configuration["AppSettings:Inventory:ReservationExpiryHours"], out var configuredHours)
            || configuredHours <= 0)
        {
            configuredHours = DefaultReservationExpiryHours;
        }
        else if (configuredHours > 24 * 14)
        {
            configuredHours = 24 * 14;
            _logger.LogWarning("AppSettings:Inventory:ReservationExpiryHours capped at 14 days (336h).");
        }
        _reservationLifetime = TimeSpan.FromHours(configuredHours);
    }

    public async Task<ResponseResult<PaystackInitializationResponse>>ExecuteAsync(string userId,
        InitializePaystackRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PaystackInitializationResponse>();
        _logger.LogInformation("Paystack checkout initialization started for user {UserId}, cart {CartId}.", userId, request.CartId);

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.AddressId) || string.IsNullOrWhiteSpace(request.CartId))
            return response.Fail("Idempotency key, email, address and cart are required.", ResponseCodes.INVALID_ACTION);

        var idempotencyKey = request.IdempotencyKey.Trim();

        if (idempotencyKey.Length > 100)
        {
            _logger.LogError("Idempotency key exceeds 100 characters for user {UserId}.", userId);
            return response.Fail("Idempotency key cannot exceed 100 characters.", ResponseCodes.INVALID_ACTION);
        }

        var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(userId, idempotencyKey, false, cancellationToken);

        if (existingOrder?.AuthorizationUrl is not null)
            return response.Success(new PaystackInitializationResponse(existingOrder.AuthorizationUrl, string.Empty, existingOrder.PaymentReference),
                "Returning the existing payment session.");

        if (existingOrder is not null)
            return response.Fail("This checkout is already being initialized. Please try again shortly.", ResponseCodes.REQUEST_IN_PROGRESS);

        if (string.IsNullOrWhiteSpace(request.DeliveryId))
            return response.Fail("A delivery option is required.", ResponseCodes.INVALID_ACTION);

        var address = await _orderRepository.GetAddressByIdAsync(request.AddressId, cancellationToken);
        if (address is null)
            return response.Fail("The selected address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var deliveryResolution = await ResolveDeliveryInfoAsync(request.DeliveryId, request.AddressId, address.State, userId, cancellationToken);
        if (deliveryResolution.ErrorMessage is not null)
            return response.Fail(deliveryResolution.ErrorMessage, ResponseCodes.INVALID_ACTION);

        if (!deliveryResolution.IsPickup
            && !await _orderRepository.AddressBelongsToUserAsync(request.AddressId, userId, cancellationToken))
        {
            _logger.LogError("User {UserId} attempted checkout with unavailable address {AddressId}.", userId, request.AddressId);
            return response.Fail("The selected address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var callbackUrl = _configuration["Frontend:PaymentCallbackUrl"];
        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri) ||
            (callbackUri.Scheme != Uri.UriSchemeHttps && callbackUri.Scheme != Uri.UriSchemeHttp))
        {
            _logger.LogCritical("Frontend:PaymentCallbackUrl is missing or invalid.");
            return response.Fail("Payment callback configuration is unavailable.", ResponseCodes.SERVICE_UNAVAILABLE);
        }

        Order? order = null;
        var reference = string.Empty;
        try
        {
            var checkoutResult = await _orderRepository.ExecuteInRetriableTransactionAsync(async (transaction, ct) =>
            {
                var cart = await _cartRepository.GetForCheckoutAsync(request.CartId, userId, ct);
                if (cart is null || cart.Items.Count == 0)
                {
                    await transaction.RollbackAsync(ct);
                    return (Order: (Order?)null, Error: "The cart is empty or unavailable.");
                }

                var checkoutItems = cart.Items.Select(item => new CheckoutItemRequest
                {
                    ProductId = item.ProductId,
                    VariantId = item.VariantId,
                    ColorId = item.ColorId,
                    Quantity = item.Quantity
                });
                var orderItems = await _checkoutOrderItemService.BuildAsync(checkoutItems, userId, ct);
                if (orderItems is null)
                {
                    await transaction.RollbackAsync(ct);
                    return (Order: (Order?)null, Error: "One or more products are unavailable.");
                }

                reference = "FS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
                var createdOrder = Order.Create(userId, idempotencyKey, request.AddressId, request.Email,
                    deliveryResolution.DeliveryMethodName, deliveryResolution.DeliveryRateId,
                    deliveryResolution.EstimatedDaysMin, deliveryResolution.EstimatedDaysMax,
                    orderItems.Sum(item => item.LineTotal), deliveryResolution.DeliveryFee, reference, orderItems,
                    paymentProvider: PaymentProviderKeys.Paystack);
                await _orderRepository.CreateWithInventoryReservationsAsync(createdOrder, DateTimeOffset.UtcNow.Add(_reservationLifetime), ct);
                cart.ClearItems();
                await _cartRepository.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return (Order: createdOrder, Error: (string?)null);
            }, cancellationToken);

            if (checkoutResult.Order is null)
            {
                return response.Fail(checkoutResult.Error!, ResponseCodes.INVALID_ACTION);
            }

            order = checkoutResult.Order;
        }
        catch (PostgresException exception) when (exception.SqlState == "P0001")
        {
            _logger.LogError(exception, "Inventory reservation was rejected for checkout {IdempotencyKey}.", idempotencyKey);
            return response.Fail("Some items are no longer available in the requested quantity.", ResponseCodes.INVALID_ACTION);
        }

        try
        {
            var amountInKobo = checked(decimal.ToInt64(order!.Total * 100m));
            var gateway = _paymentGatewayFactory.Get(PaymentProviderKeys.Paystack);
            var initialized = await gateway.InitializeAsync(
                new PaymentInitializeCommand(
                    order.Id,
                    reference,
                    order.Email,
                    address?.PhoneNumber,
                    amountInKobo,
                    order.Currency,
                    callbackUri.ToString(),
                    null),
                    cancellationToken);

            var result = new PaystackInitializationResponse(
                initialized.RedirectUrl!,
                initialized.AccessCode ?? string.Empty,
                initialized.ProviderReference);

            order.SetAuthorizationUrl(initialized.RedirectUrl!);
            await _orderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Order {OrderId} ({TrackOrderId}) initialized on Paystack with reference {Reference} and total {Total} NGN.",
                order.Id, order.TrackOrderId, reference, order.Total);
            return response.Success(result, "Payment initialized successfully.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or OverflowException)
        {
            _logger.LogError(exception,
                "Paystack initialization network call failed for order {OrderId} ({TrackOrderId}) and reference {Reference}. " +
                "Inventory reservations will NOT be released here: the 20-minute expiry worker will reclaim them, and " +
                "if Paystack actually charged the user, the incoming charge.success webhook will rescue payment.",
                order.Id, order.TrackOrderId, reference);

            await using var orderTransaction = await _orderRepository.BeginTransactionAsync(cancellationToken);
            try
            {
                order.MarkPaymentFailed(PaymentStatuses.InitializationFailed);
                await _orderRepository.SaveChangesAsync(cancellationToken);
                await orderTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await orderTransaction.RollbackAsync(cancellationToken);
                throw;
            }

            return response.Fail("Payment could not be initialized. Please try again.", ResponseCodes.SERVICE_UNAVAILABLE);
        }
    }

    private async Task<DeliveryResolutionResult?> ResolveDeliveryInfoAsync(
        string deliveryId,
        string addressId,
        string state,
        string userId,
        CancellationToken cancellationToken)
    {
        var activeContact = await _contactConfigRepository.GetActiveAsync(cancellationToken);

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
        return await _checkoutOrderItemService.BuildAsync(requestedItems, userId, cancellationToken);
    }

    private static bool IsActiveContact(FashionStore.Domain.Entities.ContactUsConfiguration contact)
    {
        return contact.IsActive;
    }
}
