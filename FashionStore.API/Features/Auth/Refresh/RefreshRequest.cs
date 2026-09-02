using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Auth.Refresh;

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
