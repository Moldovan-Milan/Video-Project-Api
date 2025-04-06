using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;

namespace OmegaStreamServices.Services.UserServices
{
        public interface IUserService
        {
                Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar);
                Task<(string refreshToken, User)> LoginUser(string email, string password, bool rememberMe);
                Task LogoutUser();

                /// <summary>
                /// This function validates the refresh token
                /// </summary>
                /// <param name="refreshToken">The value of the refresh token</param>
                /// <returns>Null if the token expired or not exist otherwise a new refresh token</returns>
                Task<(string? newRefreshToken, User? user)> LogInWithRefreshToken(string refreshToken);
                Task<List<User>> GetUsersAsync(int? pageNumber, int? pageSize);
                Task<User?> GetUserById(string id);
                Task<User?> GetUserWithFollowersById(string id);
                Task<UserWithVideosDto?> GetUserProfileWithVideos(string userId, int? pageNumber, int? pageSize);
                Task<List<UserDto?>> GetUsersByName(string name, int? pageNumber, int? pageSize);
                Task<bool> UpdateUsername(User user, string newName);

                Task<bool> SaveTheme(string? background, string? primaryColor, string? secondaryColor,
                     Stream? bannerImage, User user);
                Task<(Stream file, string contentType)> GetBannerAsync(int avatarId);
                Task DeleteAccount(string userId);
                Task<List<string>> GetRoles(string userId);
                Task VerifyUser(string userId);
                Task AddVerificationRequest(string userId);
                Task<List<UserDto>> GetVerificationRequests(int? pageNumber, int? pageSize);
                Task DeclineVerification(string userId);
                Task<bool> HasActiveVerificationRequest(string userId);
    }
}
