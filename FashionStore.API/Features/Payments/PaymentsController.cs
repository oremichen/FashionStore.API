using FashionStore.API.Features.Payments.InitializePayOnDelivery;
using FashionStore.API.Features.Payments.InitializePaystack;
using FashionStore.API.Features.Payments.ProcessPaystackWebhook;
using FashionStore.API.Features.Payments.Shared;
using FashionStore.API.Features.Payments.VerifyPaystack;

namespace FashionStore.API.Features.Payments;

[Route("api/payments")]
[ApiController]
public sealed class PaymentsController : BaseApiController
{
    private readonly IInitializePaystackService _initializePaystackService;
    private readonly IInitializePayOnDeliveryService _initializePayOnDeliveryService;
    private readonly IVerifyPaystackService _verifyPaystackService;
    private readonly IProcessPaystackWebhookService _processPaystackWebhookService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IInitializePaystackService initializePaystackService,
        IInitializePayOnDeliveryService initializePayOnDeliveryService,
        IVerifyPaystackService verifyPaystackService,
        IProcessPaystackWebhookService processPaystackWebhookService,
        ILogger<PaymentsController> logger)
    {
        _initializePaystackService = initializePaystackService;
        _initializePayOnDeliveryService = initializePayOnDeliveryService;
        _verifyPaystackService = verifyPaystackService;
        _processPaystackWebhookService = processPaystackWebhookService;
        _logger = logger;
    }

    [Authorize]
    [HttpPost("paystack/initialize")]
    [ProducesResponseType(typeof(ResponseResult<PaystackInitializationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitializePaystack([FromBody] InitializePaystackRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        return ProcessResponse(await _initializePaystackService.ExecuteAsync(userId, request, cancellationToken));
    }

    [Authorize]
    [HttpPost("payondelivery/initialize")]
    [ProducesResponseType(typeof(ResponseResult<PayOnDeliveryInitializationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> InitializePayOnDelivery(
        [FromBody] InitializePayOnDeliveryRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        return ProcessResponse(await _initializePayOnDeliveryService.ExecuteAsync(userId, request, cancellationToken));
    }

    [Authorize(Policy = "RequireAuthenticatedUserIdOrExpiredSignedUserId")]
    [HttpGet("paystack/verify/{reference}")]
    [ProducesResponseType(typeof(ResponseResult<PaymentVerificationResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyPaystack(string reference, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return ProcessResponse(new ResponseResult().Fail("You are not authorized to perform this action.", ResponseCodes.INVALID_TOKEN));
        return ProcessResponse(await _verifyPaystackService.ExecuteAsync(reference, userId, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("paystack/webhook")]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ResponseResult), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PaystackWebhook(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["x-paystack-signature"].ToString();
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        _logger.LogInformation("Received Paystack webhook request with {PayloadLength} bytes.", payload.Length);
        return ProcessResponse(await _processPaystackWebhookService.ExecuteAsync(payload, signature, cancellationToken));
    }
}
