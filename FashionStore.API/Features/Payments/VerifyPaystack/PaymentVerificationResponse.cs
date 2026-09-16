namespace FashionStore.API.Features.Payments.VerifyPaystack;

public sealed record PaymentVerificationResponse(
    string Reference,
    string OrderId,
    string Status);
