using System.Text.Json;
using FashionStore.Domain.Abstractions.Payments;
using FashionStore.Domain.Constants;

namespace FashionStore.Infrastructure.Payments;

public sealed class PaystackPaymentGateway : IPaymentGateway
{
    private const string PaystackSignatureHeader = "x-paystack-signature";
    private readonly IPaystackClient _paystackClient;

    public PaystackPaymentGateway(IPaystackClient paystackClient)
    {
        _paystackClient = paystackClient;
    }

    public string ProviderKey => PaymentProviderKeys.Paystack;

    public bool SupportsSynchronousCapture => true;

    public bool SupportsWebhooks => true;

    public async Task<PaymentInitializeResult> InitializeAsync(PaymentInitializeCommand command, CancellationToken cancellationToken)
    {
        var internalCmd = new PaystackInitializeCommand(
            command.CustomerEmail,
            command.AmountKobo,
            command.Reference,
            command.CallbackUrl);

        var result = await _paystackClient.InitializeAsync(internalCmd, cancellationToken);

        return new PaymentInitializeResult(
            ProviderReference: result.Reference,
            RedirectUrl: result.AuthorizationUrl,
            AccessCode: result.AccessCode,
            DisplayQrCode: null,
            InstructionText: null);
    }

    public async Task<PaymentVerificationResult> VerifyByMerchantReferenceAsync(string merchantReference, CancellationToken cancellationToken)
    {
        var result = await _paystackClient.VerifyAsync(merchantReference, cancellationToken);

        var isSuccess = string.Equals(result.Status, PaymentStatuses.Success, StringComparison.OrdinalIgnoreCase);

        return new PaymentVerificationResult(
            ProviderReference: result.Reference,
            MerchantReference: result.Reference,
            AmountKobo: result.Amount,
            Currency: result.Currency,
            IsSuccess: isSuccess,
            RawStatus: result.Status,
            PaidAt: result.PaidAt);
    }

    public PaymentWebhookValidationResult ValidateWebhook(string payload, IReadOnlyDictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return new PaymentWebhookValidationResult(false, string.Empty, string.Empty, null);
        }

        string? signature = null;
        foreach (var entry in headers)
        {
            if (string.Equals(entry.Key, PaystackSignatureHeader, StringComparison.OrdinalIgnoreCase))
            {
                signature = entry.Value;
                break;
            }
        }

        var signatureValid = !string.IsNullOrWhiteSpace(signature) &&
            _paystackClient.IsValidWebhookSignature(payload, signature!);

        string eventType = string.Empty;
        string providerReference = string.Empty;

        if (signatureValid)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                if (root.TryGetProperty("event", out var eventEl))
                {
                    eventType = eventEl.GetString() ?? string.Empty;
                }

                if (root.TryGetProperty("data", out var dataEl) &&
                    dataEl.TryGetProperty("reference", out var refEl))
                {
                    providerReference = refEl.GetString() ?? string.Empty;
                }
            }
            catch (JsonException)
            {
                return new PaymentWebhookValidationResult(false, string.Empty, string.Empty, payload);
            }
        }

        return new PaymentWebhookValidationResult(signatureValid, eventType, providerReference, payload);
    }

    public Task<PaymentVerificationResult?> VerifyByProviderReferenceAsync(string providerReference, CancellationToken cancellationToken)
    {
        return VerifyByProviderReferenceCore(providerReference, cancellationToken)!;
    }

    private async Task<PaymentVerificationResult> VerifyByProviderReferenceCore(string providerReference, CancellationToken cancellationToken)
    {
        return await VerifyByMerchantReferenceAsync(providerReference, cancellationToken);
    }
}
