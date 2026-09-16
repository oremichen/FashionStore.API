using System.ComponentModel.DataAnnotations;
namespace FashionStore.API.Features.Auth.AdminForgotPassword;
public sealed class AdminForgotPasswordRequest
{
    [Required] public string AdminPassword { get; init; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; init; } = string.Empty;
    [Required] public string ConfirmPassword { get; init; } = string.Empty;
}
