using Microsoft.AspNetCore.Identity;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Services
{
    public interface IUserManagerService
    {
        Task<IdentityResult> RegisterUser(string username, string email, string password, IFormFile avatar);
        Task<string> LoginUser(string email, string password);
        Task LogoutUser();
    }
}
