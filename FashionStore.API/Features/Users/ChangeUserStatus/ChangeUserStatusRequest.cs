namespace FashionStore.API.Features.Users.ChangeUserStatus;

public enum AccountStatus { Active, Deactivated, Deleted }

public sealed class ChangeUserStatusRequest
{
    public AccountStatus Status { get; init; }
}
