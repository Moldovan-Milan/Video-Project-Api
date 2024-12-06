using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Models;

namespace OmegaStreamServices.Services.UserServices
{
    public interface IUserManagerService
    {
        Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar);
        Task<string> LoginUser(string email, string password);
        Task LogoutUser();
        Task<User> GetUserById(string id);
    }
}
