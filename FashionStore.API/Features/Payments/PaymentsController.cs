using FashionStore.API.Features.Payments.InitializePaystack;
using FashionStore.API.Features.Payments.ProcessPaystackWebhook;
using FashionStore.API.Features.Payments.Shared;
using FashionStore.API.Features.Payments.VerifyPaystack;

namespace FashionStore.API.Features.Payments;

[Route("api/payments/paystack")]
[ApiController]
public sealed class PaymentsController : BaseApiController
{
    private readonly IInitializePaystackService _initializePaystackService;
    private readonly IVerifyPaystackService _verifyPaystackService;
    private readonly IProcessPaystackWebhookService _processPaystackWebhookService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IInitializePaystackService initializePaystackService,
        IVerifyPaystackService verifyPaystackService,
        IProcessPaystackWebhookService processPaystackWebhookService,
        ILogger<PaymentsController> logger)
    {
        _initializePaystackService = initializePaystackService;
        _verifyPaystackService = verifyPaystackService;
        _processPaystackWebhookService = processPaystackWebhookService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("initialize")]
    [ProducesResponseType(typeof(ResponseResult<PaystackInitializationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Initialize([FromBody] InitializePaystackRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return ProcessResponse(await _initializePaystackService.ExecuteAsync(userId, request, cancellationToken));
    }

    [Authorize]
    [HttpGet("verify/{reference}")]
    [ProducesResponseType(typeof(ResponseResult<PaymentVerificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Verify(string reference, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();
        return ProcessResponse(await _verifyPaystackService.ExecuteAsync(reference, userId, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["x-paystack-signature"].ToString();
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        _logger.LogInformation("Received Paystack webhook request with {PayloadLength} bytes.", payload.Length);
        return ProcessResponse(await _processPaystackWebhookService.ExecuteAsync(payload, signature, cancellationToken));
    }
}
