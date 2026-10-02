using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Auth.Google;

public sealed class GoogleLoginRequest
{
    [Required]
    public string AuthorizationCode { get; set; } = string.Empty;
}
