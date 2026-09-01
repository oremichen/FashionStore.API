namespace FashionStore.API.Features.Users.GetUsers;

public sealed class GetUsersResponse
{
    public required string Id { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required IReadOnlyList<UserAddressResponse> Addresses { get; init; }
    public string? PhoneNumber { get; init; }
    public required string Email { get; init; }
    public DateTimeOffset DateJoined { get; init; }
    public bool IsActiveStatus { get; init; }
    public bool IsDeletedStatus { get; init; }
}
