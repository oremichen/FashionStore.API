using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FashionStore.Application.Features.Notification
{
    public class EmailNotificationService : IEmailNotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailNotificationService> _logger;

        public EmailNotificationService(IConfiguration configuration, ILogger<EmailNotificationService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<ResponseResult> SendEmailAsync(EmailNotification notification)
        {
            var response = new ResponseResult();

            if (notification.To == null || notification.To.Count == 0)
            {
                _logger.LogError("Email request rejected because no recipient was provided.");
                return response.Fail("At least one recipient is required.", ResponseCodes.INVALID_ACTION);
            }

            _logger.LogInformation(
                "Email send requested. Subject: {Subject}. First recipient: {Recipient}. Recipient count: {RecipientCount}.",
                notification.Subject,
                notification.To[0],
                notification.To.Count);

            try
            {
                using var smtpClient = new SmtpClient();
                var host = _configuration["MailSettings:SmtpHost"]
                    ?? throw new InvalidOperationException("MailSettings:SmtpHost is not configured.");
                var port = _configuration.GetValue<int?>("MailSettings:Port")
                    ?? throw new InvalidOperationException("MailSettings:Port is not configured.");
                var username = _configuration["MailSettings:Username"];
                var password = _configuration["MailSettings:Password"];
                var enableSsl = _configuration.GetValue("MailSettings:EnableSsl", true);

                var message = BuildMimeMessage(notification);

                _logger.LogInformation(
                    "Sending email through host {Host} on port {Port} for recipient {Recipient}.",
                    host,
                    port,
                    notification.To[0]);

                await smtpClient.ConnectAsync(
                    host,
                    port,
                    SecureSocketOptions.StartTls);

                if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
                {
                    await smtpClient.AuthenticateAsync(username, password);
                }

                await smtpClient.SendAsync(message);
                await smtpClient.DisconnectAsync(true);

                _logger.LogInformation(
                    "Email sent successfully. Subject: {Subject}. First recipient: {Recipient}.",
                    notification.Subject,
                    notification.To[0]);

                return response.Success("Email sent successfully.");
            }
            catch (SmtpCommandException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to send email. Subject: {Subject}. First recipient: {Recipient}. SMTP status code: {StatusCode}.",
                    notification.Subject,
                    notification.To[0],
                    exception.StatusCode);

                return response.Fail("Error sending email.", ResponseCodes.SERVICE_UNAVAILABLE);
            }
            catch (SmtpProtocolException exception)
            {
                _logger.LogError(
                    exception,
                    "SMTP protocol error occurred while sending email. Subject: {Subject}. First recipient: {Recipient}.",
                    notification.Subject,
                    notification.To[0]);

                return response.Fail("Error sending email.", ResponseCodes.SERVICE_UNAVAILABLE);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error occurred while sending email. Subject: {Subject}. First recipient: {Recipient}.",
                    notification.Subject,
                    notification.To[0]);

                return response.Fail("An unexpected error occurred while sending email.", ResponseCodes.SYSTEM_MALFUNCTION);
            }
        }

        private MimeMessage BuildMimeMessage(EmailNotification notification)
        {
            var fromAddress = notification.From;
            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                fromAddress = _configuration["MailSettings:DefaultFromAddress"]
                    ?? throw new InvalidOperationException("MailSettings:DefaultFromAddress is not configured.");
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(fromAddress));
            AddAddresses(message.To, notification.To);
            AddAddresses(message.Cc, notification.Cc);
            AddAddresses(message.Bcc, notification.Bcc);
            message.Subject = notification.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = notification.Body
            };

            if (notification.Attchements != null)
            {
                foreach (var attachment in notification.Attchements)
                {
                    if (attachment.Attachmentfile == null || attachment.Attachmentfile.Length == 0)
                    {
                        continue;
                    }

                    bodyBuilder.Attachments.Add(attachment.FileName, attachment.Attachmentfile);
                }

                _logger.LogInformation(
                    "Email request includes {AttachmentCount} attachment(s). First recipient: {Recipient}.",
                    notification.Attchements.Count,
                    notification.To[0]);
            }

            message.Body = bodyBuilder.ToMessageBody();
            return message;
        }

        private static void AddAddresses(InternetAddressList targetCollection, List<string>? addresses)
        {
            if (addresses == null || addresses.Count == 0)
            {
                return;
            }

            foreach (var address in addresses.Where(address => !string.IsNullOrWhiteSpace(address)))
            {
                targetCollection.Add(MailboxAddress.Parse(address));
            }
        }
    }
}
