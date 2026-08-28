using FashionStore.Domain.Enums;
namespace FashionStore.Domain.Entities;
public sealed class ProductReview
{
    private ProductReview() { }
    public string Id { get; private set; } = null!;
    public string ProductId { get; private set; } = null!;
    public Product Product { get; private set; } = null!;
    public string? ReviewerId { get; private set; }
    public string ReviewerName { get; private set; } = null!;
    public string? ReviewerEmail { get; private set; }
    public string? Title { get; private set; }
    public string? Comment { get; private set; }
    public short Rating { get; private set; }
    public ReviewStatus Status { get; private set; } = ReviewStatus.Pending;
    public bool IsVerifiedPurchase { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public static ProductReview Create(string productId, string name, short rating, string? title, string? comment)
    {
        if (rating is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");
        return new ProductReview { ProductId = Rules.Required(productId, 50, nameof(productId)), ReviewerName = Rules.Required(name, 150, nameof(name)), Rating = rating, Title = Rules.Optional(title, 200, nameof(title)), Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim() };
    }
    public void Moderate(ReviewStatus status)
    {
        Status = status;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
