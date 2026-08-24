using System.Net.Http.Headers;

namespace FashionStore.Infrastructure.Notification
{
    public sealed class MailgunEmailProvider : IEmailProvider
    {
        public const string ProviderName = "Mailgun";

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MailgunEmailProvider> _logger;

        public MailgunEmailProvider(HttpClient httpClient, IConfiguration configuration, ILogger<MailgunEmailProvider> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public string Name => ProviderName;

        public async Task<EmailProviderResult> SendAsync(EmailNotification notification, CancellationToken cancellationToken = default)
        {
            var settings = _configuration.GetSection("EmailProviders:Mailgun");
            var apiKey = settings["ApiKey"];
            var domain = settings["Domain"];
            var baseUrl = settings["BaseUrl"];
            var defaultFromAddress = settings["DefaultFromAddress"];

            if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(domain) ||
                string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(defaultFromAddress))
            {
                return EmailProviderResult.Failure("Mailgun configuration is incomplete. ApiKey, Domain, BaseUrl, and DefaultFromAddress are required.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/v3/{domain}/messages");
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Basic",
                Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"api:{apiKey}")));
            request.Content = BuildContent(notification, defaultFromAddress);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return EmailProviderResult.Success();
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Mailgun rejected an email with status code {StatusCode}. Response: {ResponseBody}", (int)response.StatusCode, responseBody);
                return EmailProviderResult.Failure($"Mailgun returned HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Mailgun failed to send an email.");
                return EmailProviderResult.Failure("Mailgun could not be reached.");
            }
        }

        private static MultipartFormDataContent BuildContent(EmailNotification notification, string defaultFromAddress)
        {
            var content = new MultipartFormDataContent();
            AddString(content, "from", string.IsNullOrWhiteSpace(notification.From) ? defaultFromAddress : notification.From);
            AddAddresses(content, "to", notification.To);
            AddAddresses(content, "cc", notification.Cc);
            AddAddresses(content, "bcc", notification.Bcc);
            AddString(content, "subject", notification.Subject);
            AddString(content, "html", notification.Body);

            foreach (var attachment in notification.Attchements ?? [])
            {
                if (attachment.Attachmentfile == null || attachment.Attachmentfile.Length == 0)
                {
                    continue;
                }

                var attachmentContent = new ByteArrayContent(attachment.Attachmentfile);
                attachmentContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                content.Add(attachmentContent, "attachment", attachment.FileName);
            }

            return content;
        }

        private static void AddAddresses(MultipartFormDataContent content, string fieldName, IEnumerable<string>? addresses)
        {
            foreach (var address in addresses ?? [])
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    AddString(content, fieldName, address);
                }
            }
        }

        private static void AddString(MultipartFormDataContent content, string fieldName, string value)
        {
            content.Add(new StringContent(value), fieldName);
        }
    }
}
