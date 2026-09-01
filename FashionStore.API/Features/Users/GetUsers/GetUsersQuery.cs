namespace FashionStore.API.Features.Users.GetUsers;

public enum UserCategory { Customer, Admin }
public enum UserStatusFilter { All, Active, Deactivated, Deleted }

public sealed class GetUsersQuery
{
    public UserCategory Category { get; init; } = UserCategory.Customer;
    public UserStatusFilter Status { get; init; } = UserStatusFilter.All;
    public DateTimeOffset? From { get; init; }
    public DateTimeOffset? To { get; init; }
    public string? Search { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
