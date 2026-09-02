using System.Reflection;
using FashionStore.API.Features.Users;
using FashionStore.API.Features.Users.UpdateUser;
using FashionStore.API.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace FashionStore.IntegrationTests;

public sealed class CustomerEndpointIsolationTests
{
    [Fact]
    public void CustomerA_cannot_read_customerB_because_customer_reads_have_no_target_input()
    {
        var action = GetAction(nameof(UsersController.GetCurrentUser));
        Assert.Equal("me", action.GetCustomAttribute<HttpGetAttribute>()?.Template);
        Assert.Empty(action.GetParameters());
    }

    [Fact]
    public void CustomerA_cannot_modify_customerB_because_customer_writes_have_no_target_input()
    {
        var update = GetAction(nameof(UsersController.Update));
        Assert.Equal("me", update.GetCustomAttribute<HttpPutAttribute>()?.Template);
        Assert.DoesNotContain(update.GetParameters(), parameter =>
            parameter.Name is "userId" or "email");
        Assert.Null(typeof(UpdateUserDetailsRequest).GetProperty("CurrentEmail"));

        foreach (var name in new[] { nameof(UsersController.CreateUserAddress), nameof(UsersController.UpdateUserAddress), nameof(UsersController.DeleteUserAddress) })
            Assert.DoesNotContain(GetAction(name).GetParameters(), parameter => parameter.Name == "userId");
    }

    [Fact]
    public void Endpoints_that_target_another_user_are_explicitly_administrative()
    {
        foreach (var name in new[] { nameof(UsersController.ChangeStatus), nameof(UsersController.ResetAdminPassword), nameof(UsersController.UpdateAdminUser) })
        {
            var authorize = GetAction(name).GetCustomAttribute<AuthorizeAttribute>();
            Assert.Equal("SuperAdmin", authorize?.Roles);
        }
    }

    [Fact]
    public void Password_change_requires_an_authenticated_session()
    {
        var action = typeof(AuthController).GetMethod(nameof(AuthController.ResetPassword))
            ?? throw new InvalidOperationException("Reset-password action was not found.");
        Assert.NotNull(action.GetCustomAttribute<AuthorizeAttribute>());
        Assert.Equal("reset-password", action.GetCustomAttribute<HttpPostAttribute>()?.Template);
    }

    private static MethodInfo GetAction(string name) =>
        typeof(UsersController).GetMethod(name)
        ?? throw new InvalidOperationException($"Controller action {name} was not found.");
}
