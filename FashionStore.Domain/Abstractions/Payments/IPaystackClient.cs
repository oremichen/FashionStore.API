namespace FashionStore.Domain.Abstractions.Payments;

public sealed record PaystackInitializeCommand(string Email, long Amount, string Reference, string CallbackUrl);
public sealed record PaystackInitializeResult(string AuthorizationUrl, string AccessCode, string Reference);
public sealed record PaystackVerificationResult(string Reference, string Status, long Amount, string Currency, DateTimeOffset? PaidAt);

public interface IPaystackClient
{
    Task<PaystackInitializeResult> InitializeAsync(PaystackInitializeCommand command, CancellationToken cancellationToken);
    Task<PaystackVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken);
    bool IsValidWebhookSignature(string payload, string signature);
}
