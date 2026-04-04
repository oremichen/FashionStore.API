using System.ComponentModel.DataAnnotations;

namespace FashionStore.Application.Dtos.Request
{
    public class ConfirmEmailRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
