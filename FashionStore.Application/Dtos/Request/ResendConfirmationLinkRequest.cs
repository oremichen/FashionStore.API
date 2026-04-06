using System.ComponentModel.DataAnnotations;

namespace FashionStore.Application.Dtos.Request
{
    public class ResendConfirmationLinkRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
