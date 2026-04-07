using FashionStore.Application.Dtos.Request;
using FashionStore.Application.Dtos.Response;
using FashionStore.Shared.Common;

namespace FashionStore.Application.Abstractions.Users
{
    public interface IUserManagementService
    {
        Task<ResponseResult<UserDetailsResponse>> GetUserByEmail(string email);
        Task<ResponseResult<UserDetailsResponse>> UpdateUserDetails(UpdateUserDetailsRequest request);
        Task<ResponseResult<UserDetailsResponse>> CreateUser(CreateUserRequest request);
    }
}
