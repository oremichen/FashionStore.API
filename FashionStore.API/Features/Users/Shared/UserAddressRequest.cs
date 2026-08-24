namespace FashionStore.API.Features.Users.Shared;

public sealed class UserAddressRequest
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Landmark { get; set; }
    public bool IsMain { get; set; } = false;
}
