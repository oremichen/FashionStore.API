namespace FashionStore.API.Features.ProductReviews;
[Route("api/product-reviews")]
[ApiController]
[EnableRateLimiting(RateLimitPolicies.Submissions)]
public sealed class ProductReviewsController : BaseApiController { }
