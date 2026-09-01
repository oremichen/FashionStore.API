using System.ComponentModel.DataAnnotations;

namespace FashionStore.API.Features.Users.UpdateAdminUser;

public sealed class UpdateAdminUserRequest
{
    [Required] public string FirstName { get; init; } = string.Empty;
    [Required] public string LastName { get; init; } = string.Empty;
    [Required, EmailAddress] public string Email { get; init; } = string.Empty;
    [Required, MinLength(1)] public List<string> RoleIds { get; init; } = [];
}
