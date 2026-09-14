namespace FashionStore.API.Features.Payments.InitializePaystack;

public sealed record PaystackInitializationResponse(
    string AuthorizationUrl,
    string AccessCode,
    string Reference);
