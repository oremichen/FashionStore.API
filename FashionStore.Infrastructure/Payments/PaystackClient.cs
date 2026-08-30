using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FashionStore.Domain.Abstractions.Payments;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FashionStore.Infrastructure.Payments;

public sealed class PaystackClient : IPaystackClient
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;
    private readonly PaystackSettings _settings;
    private readonly ILogger<PaystackClient> _logger;

    public PaystackClient(HttpClient httpClient, IOptions<PaystackSettings> settings, ILogger<PaystackClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<PaystackInitializeResult> InitializeAsync(PaystackInitializeCommand command, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing Paystack transaction {Reference} for {Amount} kobo.", command.Reference, command.Amount);
        var request = new InitializeRequest(command.Email, command.Amount.ToString(CultureInfo.InvariantCulture), command.Reference, command.CallbackUrl);
        using var response = await _httpClient.PostAsJsonAsync("transaction/initialize", request, JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Paystack initialization failed for {Reference}. HTTP status {StatusCode}.",
                command.Reference, (int)response.StatusCode);
            throw new HttpRequestException("Paystack could not initialize the transaction.");
        }

        var envelope = JsonSerializer.Deserialize<PaystackEnvelope<InitializeData>>(body, JsonOptions);
        if (envelope?.Status != true || envelope.Data is null || string.IsNullOrWhiteSpace(envelope.Data.AuthorizationUrl))
        {
            _logger.LogError("Paystack returned an invalid initialization response for {Reference}.", command.Reference);
            throw new InvalidOperationException("Paystack returned an invalid initialization response.");
        }

        _logger.LogInformation("Paystack transaction {Reference} initialized successfully.", command.Reference);
        return new PaystackInitializeResult(envelope.Data.AuthorizationUrl, envelope.Data.AccessCode, envelope.Data.Reference);
    }

    public async Task<PaystackVerificationResult> VerifyAsync(string reference, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Verifying Paystack transaction {Reference}.", reference);
        using var response = await _httpClient.GetAsync("transaction/verify/" + Uri.EscapeDataString(reference), cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Paystack verification failed for {Reference}. HTTP status {StatusCode}.",
                reference, (int)response.StatusCode);
            throw new HttpRequestException("Paystack could not verify the transaction.");
        }

        var envelope = JsonSerializer.Deserialize<PaystackEnvelope<VerifyData>>(body, JsonOptions);
        if (envelope?.Status != true || envelope.Data is null)
        {
            throw new InvalidOperationException("Paystack returned an invalid verification response.");
        }

        return new PaystackVerificationResult(envelope.Data.Reference, envelope.Data.Status,
            envelope.Data.Amount, envelope.Data.Currency, envelope.Data.PaidAt);
    }

    public bool IsValidWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature)) return false;
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
        var suppliedBytes = Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant());
        var expectedBytes = Encoding.ASCII.GetBytes(expected);
        return suppliedBytes.Length == expectedBytes.Length && CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes);
    }

    private sealed record InitializeRequest(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("amount")] string Amount,
        [property: JsonPropertyName("reference")] string Reference,
        [property: JsonPropertyName("callback_url")] string CallbackUrl);

    private sealed class PaystackEnvelope<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private sealed class InitializeData
    {
        [JsonPropertyName("authorization_url")] public string AuthorizationUrl { get; set; } = string.Empty;
        [JsonPropertyName("access_code")] public string AccessCode { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
    }

    private sealed class VerifyData
    {
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        [JsonPropertyName("paid_at")] public DateTimeOffset? PaidAt { get; set; }
    }
}
