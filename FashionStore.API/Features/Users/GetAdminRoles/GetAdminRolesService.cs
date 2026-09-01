namespace FashionStore.API.Features.Users.GetAdminRoles;

public sealed record AdminRoleResponse(string Id, string Name);
public interface IGetAdminRolesService { Task<ResponseResult<IReadOnlyList<AdminRoleResponse>>> ExecuteAsync(); }

public sealed class GetAdminRolesService(RoleManager<ApplicationRole> roleManager) : IGetAdminRolesService
{
    public async Task<ResponseResult<IReadOnlyList<AdminRoleResponse>>> ExecuteAsync()
    {
        var names = RoleConstants.AdminRoles.ToArray();
        var roles = await roleManager.Roles.AsNoTracking().Where(role => role.Name != null && names.Contains(role.Name))
            .OrderBy(role => role.Name).Select(role => new AdminRoleResponse(role.Id, role.Name!)).ToListAsync();
        return new ResponseResult<IReadOnlyList<AdminRoleResponse>>().Success(roles, "Administrative roles retrieved successfully.");
    }
}
