using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.ContactUs.SubmitContact;

public sealed record SubmitContactRequest(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [MaxLength(50)] string? Phone,
    [MaxLength(50)] string? EnquiryType,
    [Required, MaxLength(5000)] string Message);
