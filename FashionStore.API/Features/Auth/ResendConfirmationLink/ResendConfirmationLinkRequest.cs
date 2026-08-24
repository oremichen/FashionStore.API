using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Auth.ResendConfirmationLink
{
    public class ResendConfirmationLinkRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
