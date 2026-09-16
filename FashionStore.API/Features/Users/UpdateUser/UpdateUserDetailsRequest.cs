using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Users.UpdateUser
{
    public class UpdateUserDetailsRequest
    {
        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? CurrentPassword { get; set; }
    }
}
