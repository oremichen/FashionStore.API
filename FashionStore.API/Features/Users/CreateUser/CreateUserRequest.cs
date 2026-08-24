using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Users.CreateUser
{
    public class CreateUserRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public List<string>? Roles { get; set; }
    }
}
