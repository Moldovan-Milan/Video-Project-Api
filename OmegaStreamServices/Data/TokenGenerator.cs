using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
       

        private readonly byte[] JWT_KEY;
        private readonly string ISSUER;

        public TokenGenerator(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
            JWT_KEY = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
            ISSUER = _configuration["Jwt:Issuer"]!;
        }
        public async Task<string> GenerateJwtToken(string userId)
        {
            User user = await _userManager.FindByIdAsync(userId);
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

            var key = new SymmetricSecurityKey(JWT_KEY);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: ISSUER,
                audience: ISSUER,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshToken(string userId)
        {
            return await Task.FromResult(new RefreshToken
            {
                Token = GenerateSecureToken(),
                UserId = userId,
                ExpiryDate = DateTime.UtcNow.AddDays(1),
            });
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
