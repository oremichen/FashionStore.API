using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.ContactUs.SubmitContact;

public sealed record SubmitContactRequest(
    [Required, MaxLength(200)] string Name,
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, Phone, MaxLength(50)] string Phone,
    [Required, MaxLength(250)] string Subject,
    [Required, MaxLength(5000)] string Message);
