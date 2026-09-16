namespace FashionStore.Domain.Abstractions.Payments;

public sealed record PaymentInitializeCommand(
    string OrderId,
    string Reference,
    string CustomerEmail,
    string? CustomerPhone,
    long AmountKobo,
    string Currency,
    string CallbackUrl,
    IReadOnlyDictionary<string, string>? Metadata);

public sealed record PaymentInitializeResult(
    string ProviderReference,
    string? RedirectUrl,
    string? AccessCode,
    string? DisplayQrCode,
    string? InstructionText);

public sealed record PaymentVerificationResult(
    string ProviderReference,
    string MerchantReference,
    long AmountKobo,
    string Currency,
    bool IsSuccess,
    string RawStatus,
    DateTimeOffset? PaidAt);

public sealed record PaymentWebhookValidationResult(
    bool IsSignatureValid,
    string EventType,
    string ProviderReference,
    string? RawPayload);

public interface IPaymentGateway
{
    string ProviderKey { get; }

    bool SupportsSynchronousCapture { get; }

    bool SupportsWebhooks { get; }

    Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeCommand command, CancellationToken cancellationToken);

    Task<PaymentVerificationResult> VerifyByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken);

    PaymentWebhookValidationResult ValidateWebhook(string payload, IReadOnlyDictionary<string, string> headers);

    Task<PaymentVerificationResult?> VerifyByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken);
}
