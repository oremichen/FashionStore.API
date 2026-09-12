using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FashionStore.Domain.Abstractions.Notification;

namespace FashionStore.API.Controllers;

[ApiController]
[Route("api/twilio-sms-message")]
[Authorize(Roles = "Admin")]
public sealed class TwilioSmsMessageController : ControllerBase
{
    private readonly ISmsProvider _smsProvider;

    public TwilioSmsMessageController(ISmsProvider smsProvider)
    {
        _smsProvider = smsProvider;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TwilioSmsMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Send([FromBody] TwilioSmsMessageRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.To) || string.IsNullOrWhiteSpace(request.Body))
        {
            return BadRequest("To and Body are required.");
        }

        var result = await _smsProvider.SendAsync(
            new SmsMessage(request.To, request.Body, request.From), cancellationToken);
        if (!result.IsSuccessful)
        {
            return StatusCode(StatusCodes.Status502BadGateway, result.Error);
        }

        return Ok(new TwilioSmsMessageResponse(result.MessageSid, "queued"));
    }
}

public sealed class TwilioSmsMessageRequest
{
    public string To { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? From { get; set; }
}

public sealed record TwilioSmsMessageResponse(string? MessageSid, string Status);
