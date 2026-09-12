namespace FashionStore.API.Features.ContactUs.Shared;

public sealed class ContactUsResponse
{
    public required string Id { get; init; }
    public required string Address { get; init; }
    public string Country { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Street { get; init; } = string.Empty;
    public required string ContactPhone { get; init; }
    public string? BusinessPhone { get; init; }
    public required string ContactEmail { get; init; }
    public string? BusinessEmail { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
    public bool IsActive { get; init; }
}
