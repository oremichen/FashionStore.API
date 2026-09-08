using FashionStore.API.Features.Payments.Shared;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Constants;
using FashionStore.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FashionStore.API.Features.Payments.InitializePaystack;

public sealed class InitializePaystackService : IInitializePaystackService
{
    private static readonly TimeSpan ReservationLifetime = TimeSpan.FromMinutes(20);
    private static readonly IReadOnlyDictionary<string, decimal> DeliveryFees = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
    {
        ["free"] = 0m,
        ["standard"] = 2500m,
        ["express"] = 7500m
    };

    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IPaystackClient _paystackClient;
    private readonly IConfiguration _configuration;
    private readonly FashionStoreDbContext _dbContext;
    private readonly ILogger<InitializePaystackService> _logger;

    public InitializePaystackService(IProductRepository productRepository, IOrderRepository orderRepository,
        IPaystackClient paystackClient, IConfiguration configuration, FashionStoreDbContext dbContext,
        ILogger<InitializePaystackService> logger)
    {
        _productRepository = productRepository;
        _orderRepository = orderRepository;
        _paystackClient = paystackClient;
        _configuration = configuration;
        _dbContext = dbContext;
        _logger = logger;
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
        
        var deliveryMethod = request.DeliveryMethod?.Trim().ToLowerInvariant() ?? string.Empty;
        
        if (!DeliveryFees.TryGetValue(deliveryMethod, out var deliveryFee))
            return response.Fail("The selected delivery method is invalid.", ResponseCodes.INVALID_ACTION);

        if (!await _orderRepository.AddressBelongsToUserAsync(request.AddressId, userId, cancellationToken))
        {
            _logger.LogError("User {UserId} attempted checkout with unavailable address {AddressId}.", userId, request.AddressId);
            return response.Fail("The selected address was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }

        var orderItems = new List<OrderItem>();
        foreach (var requestedItem in request.Items)
        {
            if (requestedItem.Quantity <= 0)
                return response.Fail("Every item quantity must be greater than zero.", ResponseCodes.INVALID_ACTION);

            var product = await _productRepository.GetByIdAsync(requestedItem.ProductId, false, cancellationToken);
            if (product is null || !product.IsActive || product.IsArchived)
                return response.Fail("One or more products are unavailable.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);

            string? colorName = null;
            string? sizeName = null;
            decimal unitPrice;

            if (!string.IsNullOrWhiteSpace(requestedItem.VariantId))
            {
                var variant = product.Variants.FirstOrDefault(item => item.Id == requestedItem.VariantId && item.IsActive);
                if (variant is null || product.AvailabilityCount < requestedItem.Quantity)
                    return response.Fail("A selected product variant is unavailable or out of stock.", ResponseCodes.INVALID_ACTION);
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
                    return response.Fail("A product selection is incomplete or out of stock.", ResponseCodes.INVALID_ACTION);
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
                    return response.Fail("A selected product color is invalid.", ResponseCodes.INVALID_ACTION);
                }
                colorName = color.Name;
            }

            if (!string.Equals(product.CurrencyCode, "NGN", StringComparison.OrdinalIgnoreCase))
                return response.Fail("Only NGN products can be paid for with this checkout.", ResponseCodes.INVALID_ACTION);
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

        var subtotal = orderItems.Sum(item => item.LineTotal);
        var reference = "FS-" + Guid.NewGuid().ToString("N").ToUpperInvariant();
        var callbackUrl = _configuration["Frontend:PaymentCallbackUrl"];
        if (!Uri.TryCreate(callbackUrl, UriKind.Absolute, out var callbackUri) ||
            (callbackUri.Scheme != Uri.UriSchemeHttps && callbackUri.Scheme != Uri.UriSchemeHttp))
        {
            _logger.LogCritical("Frontend:PaymentCallbackUrl is missing or invalid.");
            return response.Fail("Payment callback configuration is unavailable.", ResponseCodes.SERVICE_UNAVAILABLE);
        }
        var order = Order.Create(userId, idempotencyKey, request.AddressId, request.Email, deliveryMethod,
            subtotal, deliveryFee, reference, orderItems);
        try
        {
            await _orderRepository.CreateWithInventoryReservationsAsync(order, DateTimeOffset.UtcNow.Add(ReservationLifetime), cancellationToken);
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
}
