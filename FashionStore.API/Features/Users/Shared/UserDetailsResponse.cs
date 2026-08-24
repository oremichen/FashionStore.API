namespace FashionStore.API.Features.Users.Shared
{
    public class UserDetailsResponse
    {
        public string UserId { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = [];
    }
}
