﻿using Microsoft.AspNetCore.Identity;
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
        Task<User?> GetUserById(string id);
        Task<User?> GetUserWithFollowersById(string id);
        Task<UserWithVideosDto?> GetUserProfileWithVideos(string userId, int? pageNumber, int? pageSize);
        Task<List<UserDto?>> GetUsersByName(string name, int? pageNumber, int? pageSize);
        Task<bool> UpdateUsername(User user, string newName);
        Task DeleteAccount(string userId);
        Task<List<string>> GetRoles(string userId);
    }
}
