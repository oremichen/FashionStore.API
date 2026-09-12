using System.ComponentModel.DataAnnotations;
namespace FashionStore.API.Features.Users.AdminUpdateUser;
public sealed class AdminUpdateUserRequest
{
    [Required] public string AdminPassword { get; init; } = string.Empty;
    [Required] public string FirstName { get; init; } = string.Empty;
    [Required] public string LastName { get; init; } = string.Empty;
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
}
