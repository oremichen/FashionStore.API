using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FashionStore.Infrastructure.Notification;

public sealed class TwilioSmsProvider : ISmsProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TwilioSmsProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Twilio:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("Twilio:BaseUrl is required.");
        }

        _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    }

    public async Task<SmsProviderResult> SendAsync(
        SmsMessage message,
        CancellationToken cancellationToken = default)
    {
        var settings = _configuration.GetSection("Twilio");
        var accountSid = settings["AccountSid"];
        var authToken = settings["AuthToken"];
        var fromNumber = string.IsNullOrWhiteSpace(message.From)
            ? settings["FromNumber"]
            : message.From;

        if (string.IsNullOrWhiteSpace(accountSid) ||
            string.IsNullOrWhiteSpace(authToken) ||
            string.IsNullOrWhiteSpace(fromNumber))
        {
            return SmsProviderResult.Failure(
                "Twilio configuration is incomplete. AccountSid, AuthToken, and FromNumber are required.");
        }

        try
        {
            var endpoint = "2010-04-01/Accounts/" + Uri.EscapeDataString(accountSid) + "/Messages.json";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(accountSid + ":" + authToken));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["To"] = message.To,
                ["From"] = fromNumber,
                ["Body"] = message.Body
            });

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Twilio rejected SMS with status {StatusCode}.", (int)response.StatusCode);
                return SmsProviderResult.Failure("Twilio rejected the SMS request.");
            }

            var messageSid = ReadMessageSid(responseBody);
            return SmsProviderResult.Success(messageSid);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Twilio SMS delivery failed.");
            return SmsProviderResult.Failure("Twilio SMS delivery failed.");
        }
    }

    private static string? ReadMessageSid(string responseBody)
    {
        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.TryGetProperty("sid", out var sid) &&
            sid.ValueKind == JsonValueKind.String)
        {
            return sid.GetString();
        }

        return null;
    }
}
