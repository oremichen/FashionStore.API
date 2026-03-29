using System.Net;
using System.Net.Mail;
using FashionStore.Application.Abstractions.Notification;
using FashionStore.Domain.Entities;
using FashionStore.Shared.Common;
using FashionStore.Shared.Constants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
                _logger.LogWarning("Email request rejected because no recipient was provided.");
                return response.Fail("At least one recipient is required.", ResponseCodes.INVALID_ACTION);
            }

            _logger.LogInformation(
                "Email send requested. Subject: {Subject}. First recipient: {Recipient}. Recipient count: {RecipientCount}.",
                notification.Subject,
                notification.To[0],
                notification.To.Count);

            try
            {
                using var smtpClient = BuildSmtpClient();
                using var message = BuildMailMessage(notification);

                _logger.LogInformation(
                    "Sending email through host {Host} on port {Port} for recipient {Recipient}.",
                    smtpClient.Host,
                    smtpClient.Port,
                    notification.To[0]);

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation(
                    "Email sent successfully. Subject: {Subject}. First recipient: {Recipient}.",
                    notification.Subject,
                    notification.To[0]);

                return response.Success("Email sent successfully.");
            }
            catch (SmtpException exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to send email. Subject: {Subject}. First recipient: {Recipient}. SMTP status: {StatusCode}.",
                    notification.Subject,
                    notification.To[0],
                    exception.StatusCode);

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

        private SmtpClient BuildSmtpClient()
        {
            var host = _configuration["MailSettings:SmtpHost"]
                ?? throw new InvalidOperationException("MailSettings:SmtpHost is not configured.");
            var port = _configuration.GetValue<int?>("MailSettings:Port")
                ?? throw new InvalidOperationException("MailSettings:Port is not configured.");
            var username = _configuration["MailSettings:Username"];
            var password = _configuration["MailSettings:Password"];
            var enableSsl = _configuration.GetValue("MailSettings:EnableSsl", true);

            var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl
            };

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                smtpClient.Credentials = new NetworkCredential(username, password);
            }

            return smtpClient;
        }

        private MailMessage BuildMailMessage(EmailNotification notification)
        {
            var fromAddress = notification.From;
            if (string.IsNullOrWhiteSpace(fromAddress))
            {
                fromAddress = _configuration["MailSettings:DefaultFromAddress"]
                    ?? throw new InvalidOperationException("MailSettings:DefaultFromAddress is not configured.");
            }

            var message = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = notification.Subject,
                Body = notification.Body,
                IsBodyHtml = true
            };

            AddAddresses(message.To, notification.To);
            AddAddresses(message.CC, notification.Cc);
            AddAddresses(message.Bcc, notification.Bcc);

            if (notification.Attchements != null)
            {
                foreach (var attachment in notification.Attchements)
                {
                    if (attachment.Attachmentfile == null || attachment.Attachmentfile.Length == 0)
                    {
                        continue;
                    }

                    var stream = new MemoryStream(attachment.Attachmentfile);
                    var mailAttachment = new System.Net.Mail.Attachment(stream, attachment.FileName);
                    message.Attachments.Add(mailAttachment);
                }

                _logger.LogInformation(
                    "Email request includes {AttachmentCount} attachment(s). First recipient: {Recipient}.",
                    notification.Attchements.Count,
                    notification.To[0]);
            }

            return message;
        }

        private static void AddAddresses(MailAddressCollection targetCollection, List<string>? addresses)
        {
            if (addresses == null || addresses.Count == 0)
            {
                return;
            }

            foreach (var address in addresses.Where(address => !string.IsNullOrWhiteSpace(address)))
            {
                targetCollection.Add(new MailAddress(address));
            }
        }
    }
}
