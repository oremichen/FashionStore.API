using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FashionStore.Domain.Abstractions.Notification;
using FashionStore.Shared.Common;
using FashionStore.Shared.Constants;

namespace FashionStore.API.Controllers;

[ApiController]
[Route("api/twilio-sms-message")]
[Authorize(Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.BusinessAdmin}")]
public sealed class TwilioSmsMessageController : BaseApiController
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
            return ProcessResponse(new ResponseResult().Fail(
                "To and Body are required.", ResponseCodes.INVALID_ACTION));
        }

        var result = await _smsProvider.SendAsync(
            new SmsMessage(request.To, request.Body, request.From), cancellationToken);
        if (!result.IsSuccessful)
        {
            return ProcessResponse(new ResponseResult().Fail(
                "The SMS could not be sent. Please try again later.", ResponseCodes.GATEWAY_TIMEOUT));
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
