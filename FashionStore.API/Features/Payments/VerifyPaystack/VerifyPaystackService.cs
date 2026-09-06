using FashionStore.API.Features.Payments.Shared;
using FashionStore.Domain.Abstractions.Orders;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Constants;

namespace FashionStore.API.Features.Payments.VerifyPaystack;

public sealed class VerifyPaystackService : IVerifyPaystackService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaystackClient _paystackClient;
    private readonly ILogger<VerifyPaystackService> _logger;

    public VerifyPaystackService(IOrderRepository orderRepository, IPaystackClient paystackClient, ILogger<VerifyPaystackService> logger)
    {
        _orderRepository = orderRepository;
        _paystackClient = paystackClient;
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

        PaystackVerificationResult transaction;
        try
        {
            transaction = await _paystackClient.VerifyAsync(reference, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or OverflowException)
        {
            _logger.LogError(exception, "Paystack verify API call failed for order {OrderId} and reference {Reference}.", order.Id, reference);
            return response.Fail("Payment verification is temporarily unavailable.", ResponseCodes.SERVICE_UNAVAILABLE);
        }

        var expectedAmount = checked(decimal.ToInt64(order.Total * 100m));
        var detailsMatch = transaction.Reference == order.PaymentReference &&
            transaction.Amount == expectedAmount && string.Equals(transaction.Currency, order.Currency, StringComparison.OrdinalIgnoreCase);
        if (!detailsMatch)
        {
            _logger.LogError("Paystack verification mismatch for order {OrderId}. Expected {Amount} {Currency}; received {PaidAmount} {PaidCurrency}.",
                order.Id, expectedAmount, order.Currency, transaction.Amount, transaction.Currency);
            return response.Fail("Payment details did not match the order.", ResponseCodes.SECURITY_VIOLATION);
        }

        var paymentSucceeded = string.Equals(transaction.Status, PaymentStatuses.Success, StringComparison.OrdinalIgnoreCase);
        var paidAt = transaction.PaidAt ?? DateTimeOffset.UtcNow;
        var failedStatus = paymentSucceeded ? PaymentStatuses.Failed : (transaction.Status ?? PaymentStatuses.Failed);

        await using var orderTransaction = await _orderRepository.BeginTransactionAsync(cancellationToken);
        try
        {
            if (paymentSucceeded)
            {
                order.MarkPaid(paidAt);
                foreach (var reservation in order.InventoryReservations.Where(item => item.Status == InventoryReservationStatuses.Reserved))
                {
                    await _orderRepository.ConsumeInventoryReservationAsync(reservation.Id, cancellationToken);
                }
            }
            else
            {
                order.MarkPaymentFailed(failedStatus);
                foreach (var reservation in order.InventoryReservations.Where(item => item.Status == InventoryReservationStatuses.Reserved))
                {
                    await _orderRepository.ReleaseInventoryReservationAsync(reservation.Id, InventoryReservationStatuses.Released, cancellationToken);
                }
            }

            await _orderRepository.SaveChangesAsync(cancellationToken);
            await orderTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await orderTransaction.RollbackAsync(cancellationToken);
            throw;
        }

        _logger.LogInformation("Payment reference {Reference} for order {OrderId} verified with status {Status}.", reference, order.Id, order.PaymentStatus);
        return response.Success(new PaymentVerificationResponse(reference, order.Id, order.PaymentStatus), "Payment verification completed.");
    }
}
