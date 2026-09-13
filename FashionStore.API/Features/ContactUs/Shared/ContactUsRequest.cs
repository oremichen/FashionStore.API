using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.ContactUs.Shared;

public class ContactUsRequest
{
    [Required, StringLength(100)]
    public string Country { get; init; } = string.Empty;

    [Required, StringLength(100)]
    public string State { get; init; } = string.Empty;

    [StringLength(100)]
    public string? City { get; init; }

    [StringLength(250)]
    public string? Street { get; init; }

    [Required, StringLength(50)]
    public string ContactPhone { get; init; } = string.Empty;

    [StringLength(50)]
    public string? BusinessPhone { get; init; }

    [Required, StringLength(254), EmailAddress]
    public string ContactEmail { get; init; } = string.Empty;

    [StringLength(254), EmailAddress]
    public string? BusinessEmail { get; init; }

    public bool IsActive { get; init; }
}
