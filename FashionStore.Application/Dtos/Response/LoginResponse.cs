namespace FashionStore.Application.Dtos.Response
{
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;

        public DateTimeOffset ExpiresAtUtc { get; set; }

        public string TokenType { get; set; } = "Bearer";
    }
}
