using System.Net.Mail;

namespace FashionStore.API.Controllers;

[ApiController]
[Route("api/amazonemailapi")]
public sealed class AmazonEmailApiController(IEmailProvider emailProvider) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [EndpointSummary("Send a test email through Amazon SES SMTP")]
    public async Task<IActionResult> SendTestEmail(
        [FromBody] AmazonEmailRequest request,
        CancellationToken cancellationToken)
    {
        var emails = (request.Emails ?? [])
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (emails.Count == 0)
        {
            return BadRequest(new { message = "At least one email address is required." });
        }

        var invalidEmails = emails.Where(email => !MailAddress.TryCreate(email, out _)).ToArray();
        if (invalidEmails.Length > 0)
        {
            return BadRequest(new { message = "One or more email addresses are invalid.", invalidEmails });
        }

        var notification = new EmailNotification
        {
            To = emails,
            Cc = [],
            Bcc = [],
            Subject = string.IsNullOrWhiteSpace(request.Subject)
                ? "Amazon SES email test"
                : request.Subject.Trim(),
            Body = string.IsNullOrWhiteSpace(request.Body)
                ? "<p>Amazon SES SMTP is configured correctly.</p>"
                : request.Body,
            Attchements = []
        };

        var result = await emailProvider.SendAsync(notification, cancellationToken);
        if (!result.IsSuccessful)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                message = "Amazon SES could not send the test email.",
                error = result.Error
            });
        }

        return Ok(new
        {
            message = "Test email sent successfully through Amazon SES.",
            recipients = emails
        });
    }

    public sealed class AmazonEmailRequest
    {
        public List<string> Emails { get; init; } = [];
        public string? Subject { get; init; }
        public string? Body { get; init; }
    }
}
