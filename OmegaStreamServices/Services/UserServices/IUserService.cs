using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;

namespace OmegaStreamServices.Services.UserServices
{
    public interface IUserService
    {
        Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar);
        Task<(string accesToken, string refreshToken, User)> LoginUser(string email, string password, bool rememberMe);
        Task LogoutUser();
        Task<(string? accessToken, string? newRefreshToken, User? user)> GenerateJwtWithRefreshToken(string refreshToken);
        Task<User?> GetUserById(string id);
        Task<User?> GetUserWithFollowersById(string id);
        Task<UserWithVideosDto?> GetUserProfileWithVideos(string userId, int? pageNumber, int? pageSize);
        Task<List<UserDto?>> GetUsersByName(string name, int? pageNumber, int? pageSize);
        Task<bool> UpdateUsername(User user, string newName);
        Task DeleteAccount(string userId);
        Task<List<string>> GetRoles(string userId);
    }
}
