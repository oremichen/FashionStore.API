using FashionStore.Application.Dtos;
using FashionStore.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FashionStore.Application.Features.Auth
{
    public class AuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AuthService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task Login(LoginRequest login)
        {

            _userManager.GenerateUserTokenAsync(new ApplicationUser { Email = login.Email }, "Default", "Login");
        }
    }
}
