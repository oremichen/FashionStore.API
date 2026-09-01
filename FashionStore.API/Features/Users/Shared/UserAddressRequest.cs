using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Users.Shared;

public sealed class UserAddressRequest
{
    [Required, StringLength(250)]
    public string Street { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string State { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Country { get; set; } = string.Empty;

    [StringLength(20)]
    public string? PostalCode { get; set; }

    [Required, Phone, StringLength(50)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Landmark { get; set; }

    public bool IsMain { get; set; } = false;
}
