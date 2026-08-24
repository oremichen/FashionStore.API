using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Auth.Login
{
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
