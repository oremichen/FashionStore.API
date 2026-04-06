namespace FashionStore.Application.Dtos.Response
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public string TokenType { get; set; } = "Bearer";

        public string UserFirstName { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public List<string> UserRoles { get; set; } = [];
    }
}
