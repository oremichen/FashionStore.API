using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Users.UpdateUser
{
    public class UpdateUserDetailsRequest
    {
        [Required]
        [EmailAddress]
        public string CurrentEmail { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }
    }
}
