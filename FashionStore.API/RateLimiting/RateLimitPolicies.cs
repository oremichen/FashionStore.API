namespace FashionStore.API.RateLimiting;

public static class RateLimitPolicies
{
    public const string Authentication = "authentication";
    public const string Registration = "registration";
    public const string Submissions = "submissions";
    public const string ProductListing = "product-listing";
    public const string Cart = "cart";
    public const string Checkout = "checkout";
    public const string AdminUpload = "admin-upload";
    public const string PaymentWebhook = "payment-webhook";
}
