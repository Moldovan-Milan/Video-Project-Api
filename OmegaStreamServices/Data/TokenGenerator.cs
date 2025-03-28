using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Data
{
    public class TokenGenerator
    {
        private readonly UserManager<User> _userManager;

        public TokenGenerator(UserManager<User> userManager)
        {
            _userManager = userManager;
        }
        public async Task<string> GenerateJwtToken(User user, byte[] JwtKey, string issuer)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(JwtKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: issuer,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static RefreshToken GenerateRefreshToken(string userId)
        {
            return new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(30),
            };
        }

        private static string GenerateSecureToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            return Convert.ToBase64String(randomNumber);
        }
    }
}
