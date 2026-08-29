using System.Text.Json;
using FashionStore.API.Features.Payments.VerifyPaystack;
using FashionStore.Domain.Abstractions.Payments;

namespace FashionStore.API.Features.Payments.ProcessPaystackWebhook;

public sealed class ProcessPaystackWebhookService : IProcessPaystackWebhookService
{
    private readonly IPaystackClient _paystackClient;
    private readonly IVerifyPaystackService _verifyPaystackService;
    private readonly ILogger<ProcessPaystackWebhookService> _logger;

    public ProcessPaystackWebhookService(IPaystackClient paystackClient, IVerifyPaystackService verifyPaystackService,
        ILogger<ProcessPaystackWebhookService> logger)
    {
        _paystackClient = paystackClient;
        _verifyPaystackService = verifyPaystackService;
        _logger = logger;
    }

    public async Task<ResponseResult> ExecuteAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        var response = new ResponseResult();
        if (!_paystackClient.IsValidWebhookSignature(payload, signature))
        {
            _logger.LogWarning("Rejected Paystack webhook with an invalid signature.");
            return response.Fail("Invalid webhook signature.", ResponseCodes.SECURITY_VIOLATION);
        }

        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;
            var eventName = root.TryGetProperty("event", out var eventElement) ? eventElement.GetString() : null;
            if (!string.Equals(eventName, "charge.success", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Ignored Paystack webhook event {EventName}.", eventName ?? "unknown");
                return response.Success("Webhook acknowledged.");
            }

            if (!root.TryGetProperty("data", out var dataElement) ||
                !dataElement.TryGetProperty("reference", out var referenceElement) ||
                string.IsNullOrWhiteSpace(referenceElement.GetString()))
            {
                return response.Fail("Webhook payment reference is missing.", ResponseCodes.INVALID_REFERENCE_PROVIDED);
            }

            var reference = referenceElement.GetString()!;
            _logger.LogInformation("Processing Paystack charge.success webhook for reference {Reference}.", reference);
            var verification = await _verifyPaystackService.ExecuteAsync(reference, null, cancellationToken);
            if (!verification.IsSuccessful) return response.Fail(verification.Description, verification.StatusCode);
            return response.Success("Webhook processed successfully.");
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Rejected malformed Paystack webhook payload.");
            return response.Fail("Webhook payload is invalid.", ResponseCodes.INVALID_ACTION);
        }
    }
}
