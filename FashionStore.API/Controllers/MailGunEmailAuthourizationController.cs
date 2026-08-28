using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;

namespace FashionStore.API.Controllers;

[ApiController]
[Route("api/mailgun-email-authourization")]
public sealed class MailGunEmailAuthourizationController(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MailGunEmailAuthourizationController> logger) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [EndpointSummary("Authorize multiple Mailgun sandbox recipients")]
    public async Task<IActionResult> AuthorizeRecipients(
        [FromBody] MailGunEmailAuthourizationRequest request,
        CancellationToken cancellationToken)
    {
        var emails = (request.Emails ?? [])
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (emails.Length == 0)
        {
            return BadRequest(new { message = "At least one email address is required." });
        }

        var invalidEmails = emails.Where(email => !MailAddress.TryCreate(email, out _)).ToArray();
        if (invalidEmails.Length > 0)
        {
            return BadRequest(new { message = "One or more email addresses are invalid.", invalidEmails });
        }

        var settings = configuration.GetSection("EmailProviders:Mailgun");
        var apiKey = settings["ApiKey"];
        var baseUrl = settings["BaseUrl"];
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
        {
            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                detail: "Mailgun ApiKey and BaseUrl must be configured.");
        }

        var authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"api:{apiKey}")));
        var client = httpClientFactory.CreateClient();
        var tasks = emails.Select(email => AuthorizeRecipient(email, baseUrl, authorization, client, cancellationToken));
        var results = await Task.WhenAll(tasks);
        var allSuccessful = results.All(result => result.IsSuccessful);

        return StatusCode(
            allSuccessful ? StatusCodes.Status200OK : StatusCodes.Status502BadGateway,
            new
            {
                message = allSuccessful
                    ? "Mailgun authorization requests were created. Each recipient must click the verification link sent by Mailgun."
                    : "One or more Mailgun authorization requests failed.",
                results
            });
    }

    private async Task<MailGunEmailAuthourizationResult> AuthorizeRecipient(
        string email,
        string baseUrl,
        AuthenticationHeaderValue authorization,
        HttpClient client,
        CancellationToken cancellationToken)
    {
        try
        {
            using var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"{baseUrl.TrimEnd('/')}/v5/sandbox/auth_recipients?email={Uri.EscapeDataString(email)}");
            httpRequest.Headers.Authorization = authorization;
            httpRequest.Content = new StringContent(string.Empty);

            using var response = await client.SendAsync(httpRequest, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Mailgun recipient authorization failed for {Email} with status code {StatusCode}. Response: {ResponseBody}",
                    email,
                    (int)response.StatusCode,
                    responseBody);
            }

            return new MailGunEmailAuthourizationResult(
                email,
                response.IsSuccessStatusCode,
                (int)response.StatusCode,
                responseBody);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Mailgun recipient authorization could not be completed for {Email}.", email);
            return new MailGunEmailAuthourizationResult(email, false, null, "Mailgun could not be reached.");
        }
    }

    public sealed class MailGunEmailAuthourizationRequest
    {
        public List<string> Emails { get; init; } = [];
    }

    public sealed record MailGunEmailAuthourizationResult(
        string Email,
        bool IsSuccessful,
        int? StatusCode,
        string Response);
}
