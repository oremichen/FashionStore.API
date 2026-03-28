using System.ComponentModel.DataAnnotations;

namespace FashionStore.Application.Dtos
{
    public class LoginRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
