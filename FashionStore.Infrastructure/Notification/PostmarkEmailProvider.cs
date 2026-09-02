using System.Net.Mail;
using PostmarkDotNet;

namespace FashionStore.Infrastructure.Notification
{
    public sealed class PostmarkEmailProvider : IEmailProvider
    {
        public const string ProviderName = "Postmark";

        private readonly IConfiguration _configuration;
        private readonly ILogger<PostmarkEmailProvider> _logger;

        public PostmarkEmailProvider(
            IConfiguration configuration,
            ILogger<PostmarkEmailProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string Name => ProviderName;

        public async Task<EmailProviderResult> SendAsync(
            EmailNotification notification,
            CancellationToken cancellationToken = default)
        {
            var settings = _configuration.GetSection("EmailProviders:Postmark");
            var serverToken = settings["ServerToken"];
            var defaultFromAddress = settings["DefaultFromAddress"];
            var defaultFromName = settings["DefaultFromName"];

            if (string.IsNullOrWhiteSpace(serverToken) ||
                string.IsNullOrWhiteSpace(defaultFromAddress))
            {
                return EmailProviderResult.Failure(
                    "Postmark configuration is incomplete. ServerToken and DefaultFromAddress are required.");
            }

            try
            {
                var fromAddress = string.IsNullOrWhiteSpace(notification.From)
                    ? defaultFromAddress
                    : notification.From;
                var message = new PostmarkMessage
                {
                    To = JoinAddresses(notification.To),
                    Cc = JoinAddresses(notification.Cc),
                    Bcc = JoinAddresses(notification.Bcc),
                    From = string.IsNullOrWhiteSpace(defaultFromName)
                        ? fromAddress
                        : new MailAddress(fromAddress, defaultFromName).ToString(),
                    ReplyTo = notification.ReplyTo,
                    Subject = notification.Subject,
                    HtmlBody = notification.Body,
                    MessageStream = settings["MessageStream"] ?? "outbound",
                    TrackOpens = settings.GetValue("TrackOpens", true)
                };

                foreach (var attachment in notification.Attchements ?? [])
                {
                    if (attachment.Attachmentfile == null || attachment.Attachmentfile.Length == 0)
                    {
                        continue;
                    }

                    message.AddAttachment(
                        attachment.Attachmentfile,
                        attachment.FileName,
                        GetContentType(attachment.FileName));
                }

                var client = new PostmarkClient(serverToken);
                var result = await client.SendMessageAsync(message);
                if (result.Status == PostmarkStatus.Success)
                {
                    return EmailProviderResult.Success();
                }

                _logger.LogWarning(
                    "Postmark rejected an email. Status: {Status}. Error code: {ErrorCode}. Message: {Message}",
                    result.Status, result.ErrorCode, result.Message);
                return EmailProviderResult.Failure(
                    $"Postmark delivery failed: {result.Message ?? result.Status.ToString()}.");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Postmark failed to send an email.");
                return EmailProviderResult.Failure("Postmark email delivery failed.");
            }
        }

        private static string? JoinAddresses(IEnumerable<string>? addresses)
        {
            var value = string.Join(",", addresses?.Where(address => !string.IsNullOrWhiteSpace(address)) ?? []);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static string GetContentType(string fileName) =>
            Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".gif" => "image/gif",
                ".jpeg" or ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
    }
}
