using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace FashionStore.Infrastructure.Notification
{
    public sealed class AmazonSesEmailProvider : IEmailProvider
    {
        public const string ProviderName = "AmazonSes";

        private readonly IConfiguration _configuration;
        private readonly ILogger<AmazonSesEmailProvider> _logger;

        public AmazonSesEmailProvider(
            IConfiguration configuration,
            ILogger<AmazonSesEmailProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public string Name => ProviderName;

        public async Task<EmailProviderResult> SendAsync(
            EmailNotification notification,
            CancellationToken cancellationToken = default)
        {
            var settings = _configuration.GetSection("EmailProviders:AmazonSes");
            var host = settings["Host"];
            var username = settings["Username"];
            var password = settings["Password"];
            var defaultFromAddress = settings["DefaultFromAddress"];
            var defaultFromName = settings["DefaultFromName"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(defaultFromAddress) ||
                !int.TryParse(settings["Port"], out var port))
            {
                return EmailProviderResult.Failure(
                    "Amazon SES SMTP configuration is incomplete. Host, Port, Username, Password, and DefaultFromAddress are required.");
            }

            try
            {
                using var message = BuildMessage(notification, defaultFromAddress, defaultFromName);
                var configurationSet = settings["ConfigurationSet"];
                if (!string.IsNullOrWhiteSpace(configurationSet))
                {
                    message.Headers.Add("X-SES-CONFIGURATION-SET", configurationSet);
                }

                using var smtpClient = new SmtpClient(host, port)
                {
                    EnableSsl = settings.GetValue("EnableSsl", true),
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password),
                    DeliveryMethod = SmtpDeliveryMethod.Network
                };

                await smtpClient.SendMailAsync(message, cancellationToken);
                return EmailProviderResult.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Amazon SES failed to send an email through SMTP.");
                return EmailProviderResult.Failure("Amazon SES SMTP delivery failed.");
            }
        }

        private static MailMessage BuildMessage(
            EmailNotification notification,
            string defaultFromAddress,
            string? defaultFromName)
        {
            var fromAddress = string.IsNullOrWhiteSpace(notification.From)
                ? defaultFromAddress
                : notification.From;
            var message = new MailMessage
            {
                From = string.IsNullOrWhiteSpace(defaultFromName)
                    ? new MailAddress(fromAddress)
                    : new MailAddress(fromAddress, defaultFromName),
                Subject = notification.Subject,
                Body = notification.Body,
                IsBodyHtml = true
            };

            AddAddresses(message.To, notification.To);
            AddAddresses(message.CC, notification.Cc);
            AddAddresses(message.Bcc, notification.Bcc);

            if (!string.IsNullOrWhiteSpace(notification.ReplyTo))
            {
                message.ReplyToList.Add(notification.ReplyTo);
            }

            foreach (var attachment in notification.Attchements ?? [])
            {
                if (attachment.Attachmentfile == null || attachment.Attachmentfile.Length == 0)
                {
                    continue;
                }

                message.Attachments.Add(new System.Net.Mail.Attachment(
                    new MemoryStream(attachment.Attachmentfile),
                    attachment.FileName,
                    MediaTypeNames.Application.Octet));
            }

            return message;
        }

        private static void AddAddresses(MailAddressCollection destination, IEnumerable<string>? addresses)
        {
            foreach (var address in addresses ?? [])
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    destination.Add(address);
                }
            }
        }
    }
}
