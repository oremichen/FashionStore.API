using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.ContactUs.Shared;

public class ContactUsRequest
{
    [Required, StringLength(500)]
    public string Address { get; init; } = string.Empty;

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
