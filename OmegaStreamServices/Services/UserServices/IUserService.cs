using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Models;

namespace OmegaStreamServices.Services.UserServices
{
    public interface IUserService
    {
        Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar);
        Task<(string, string)> LoginUser(string email, string password, bool rememberMe);
        Task LogoutUser();
        Task<string?> GenerateJwtWithRefreshToken(string refreshToken);
        Task<User> GetUserById(string id);
    }
}
