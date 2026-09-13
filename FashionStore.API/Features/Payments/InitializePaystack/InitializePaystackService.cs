using FashionStore.API.Features.Payments.Shared;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Delivery;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Constants;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FashionStore.API.Features.Payments.InitializePaystack;

public sealed class InitializePaystackService : IInitializePaystackService
{
    private const double DefaultReservationExpiryHours = 24.0;
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IDeliveryRepository _deliveryRepository;
    private readonly IPaystackClient _paystackClient;
    private readonly IConfiguration _configuration;
    private readonly FashionStoreDbContext _dbContext;
    private readonly ILogger<InitializePaystackService> _logger;
    private readonly TimeSpan _reservationLifetime;

    public InitializePaystackService(IProductRepository productRepository, IOrderRepository orderRepository,
        IPaystackClient paystackClient, IConfiguration configuration, FashionStoreDbContext dbContext, IDeliveryRepository deliveryRepository,
        ILogger<InitializePaystackService> logger)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _deliveryRepository = deliveryRepository;
        _paystackClient = paystackClient;
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;

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

    public async Task<ResponseResult<PaystackInitializationResponse>> ExecuteAsync(string userId,
        InitializePaystackRequest request, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PaystackInitializationResponse>();
        _logger.LogInformation("Paystack checkout initialization started for user {UserId} with {ItemCount} items.", userId, request.Items.Count);

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.AddressId) || request.Items.Count == 0)
            return response.Fail("Idempotency key, email, address and at least one order item are required.", ResponseCodes.INVALID_ACTION);

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

        var address = await _dbContext.Addresses.FindAsync([request.AddressId], cancellationToken);
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

        var orderItems = await BuildOrderItemsAsync(request.Items, userId, cancellationToken);
        if (orderItems is null)
            return response.Fail("One or more products are unavailable.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

        var subtotal = orderItems.Sum(item => item.LineTotal);
        var reference = "FS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        var callbackUrl = _configuration["Frontend:PaymentCallbackUrl"];
        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri) ||
            (callbackUri.Scheme != Uri.UriSchemeHttps && callbackUri.Scheme != Uri.UriSchemeHttp))
        {
            _logger.LogCritical("Frontend:PaymentCallbackUrl is missing or invalid.");
            return response.Fail("Payment callback configuration is unavailable.", ResponseCodes.SERVICE_UNAVAILABLE);
        }

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
            orderItems);

        try
        {
            await _orderRepository.CreateWithInventoryReservationsAsync(order, DateTimeOffset.UtcNow.Add(_reservationLifetime), cancellationToken);
        }
        catch (PostgresException exception) when (exception.SqlState == "P0001")
        {
            _logger.LogError(exception, "Inventory reservation was rejected for checkout {IdempotencyKey}.", idempotencyKey);
            return response.Fail(exception.MessageText, ResponseCodes.INVALID_ACTION);
        }

        try
        {
            var amountInKobo = checked(decimal.ToInt64(order.Total * 100m));
            var initialized = await _paystackClient.InitializeAsync(
                new PaystackInitializeCommand(
                    order.Email,
                    amountInKobo,
                    reference,
                    callbackUri.ToString()),
                    cancellationToken);

            var result = new PaystackInitializationResponse(
                initialized.AuthorizationUrl,
                initialized.AccessCode,
                initialized.Reference);

            order.SetAuthorizationUrl(initialized.AuthorizationUrl);
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
        var activeContacts = await _dbContext.ContactUsConfigurations.AsNoTracking()
            .ToListAsync(cancellationToken);
        var activeContact = activeContacts.SingleOrDefault(IsActiveContact);

        var pickupMethod = await _deliveryRepository.GetMethodByIdAsync(deliveryId, cancellationToken);
        var isPickupOrder = pickupMethod is not null
            && pickupMethod.Name.Contains("pickup", StringComparison.OrdinalIgnoreCase)
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

        var isPickupAddress = activeContact?.AddressId == addressId &&
            requestedMethod?.Name.Contains("pickup", StringComparison.OrdinalIgnoreCase) == true;

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

    private static bool IsActiveContact(FashionStore.Domain.Entities.ContactUsConfiguration contact)
    {
        return contact.IsActive;
    }

    private sealed class DeliveryResolutionResult
    {
        public bool IsPickup { get; set; }
        public string DeliveryMethodName { get; set; } = null!;
        public string DeliveryRateId { get; set; } = null!;
        public int? EstimatedDaysMin { get; set; }
        public int? EstimatedDaysMax { get; set; }
        public decimal DeliveryFee { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
