using FashionStore.API.Features.Payments.Shared;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Abstractions.Products;
using FashionStore.Domain.Constants;

namespace FashionStore.API.Features.Payments.VerifyPaystack;

public sealed class VerifyPaystackService : IVerifyPaystackService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaystackClient _paystackClient;
    private readonly IProductRepository _productRepository;
    private readonly ILogger<VerifyPaystackService> _logger;

    public VerifyPaystackService(IOrderRepository orderRepository, IPaystackClient paystackClient,
        IProductRepository productRepository, ILogger<VerifyPaystackService> logger)
    {
        _orderRepository = orderRepository;
        _paystackClient = paystackClient;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<ResponseResult<PaymentVerificationResponse>> ExecuteAsync(string reference, string? userId, CancellationToken cancellationToken)
    {
        var response = new ResponseResult<PaymentVerificationResponse>();
        if (string.IsNullOrWhiteSpace(reference))
            return response.Fail("Payment reference is required.", ResponseCodes.INVALID_REFERENCE_PROVIDED);

        var order = await _orderRepository.GetByPaymentReferenceAsync(reference, true, cancellationToken);
        if (order is null || (userId is not null && order.UserId != userId))
        {
            _logger.LogWarning("Payment verification could not locate reference {Reference} for user {UserId}.", reference, userId);
            return response.Fail("Payment reference was not found.", ResponseCodes.UNABLE_TO_LOCATE_RECORD);
        }
        if (order.PaymentStatus == PaymentStatuses.Success)
            return response.Success(new PaymentVerificationResponse(reference, order.Id, PaymentStatuses.Success), "Payment already verified.");

        try
        {
            var transaction = await _paystackClient.VerifyAsync(reference, cancellationToken);
            var expectedAmount = checked(decimal.ToInt64(order.Total * 100m));
            var detailsMatch = transaction.Reference == order.PaymentReference &&
                transaction.Amount == expectedAmount && string.Equals(transaction.Currency, order.Currency, StringComparison.OrdinalIgnoreCase);
            if (!detailsMatch)
            {
                _logger.LogError("Paystack verification mismatch for order {OrderId}. Expected {Amount} {Currency}; received {PaidAmount} {PaidCurrency}.",
                    order.Id, expectedAmount, order.Currency, transaction.Amount, transaction.Currency);
                return response.Fail("Payment details did not match the order.", ResponseCodes.SECURITY_VIOLATION);
            }

            if (string.Equals(transaction.Status, PaymentStatuses.Success, StringComparison.OrdinalIgnoreCase))
            {
                order.MarkPaid(transaction.PaidAt ?? DateTimeOffset.UtcNow);
                foreach (var reservation in order.InventoryReservations.Where(item => item.Status == InventoryReservationStatuses.Reserved))
                    reservation.Consume(transaction.PaidAt ?? DateTimeOffset.UtcNow);
            }
            else
            {
                order.MarkPaymentFailed(transaction.Status);
                foreach (var reservation in order.InventoryReservations.Where(item => item.Status == InventoryReservationStatuses.Reserved))
                {
                    reservation.Release(DateTimeOffset.UtcNow);
                    var product = await _productRepository.GetByIdAsync(reservation.ProductId, true, cancellationToken);
                    product?.ReleaseStock(reservation.Quantity);
                }
            }
            await _orderRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Payment reference {Reference} for order {OrderId} verified with status {Status}.", reference, order.Id, order.PaymentStatus);
            return response.Success(new PaymentVerificationResponse(reference, order.Id, order.PaymentStatus), "Payment verification completed.");
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or OverflowException)
        {
            _logger.LogError(exception, "Payment verification failed for order {OrderId} and reference {Reference}.", order.Id, reference);
            return response.Fail("Payment verification is temporarily unavailable.", ResponseCodes.SERVICE_UNAVAILABLE);
        }
    }
}
