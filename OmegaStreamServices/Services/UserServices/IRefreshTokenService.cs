using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.UserServices
{
    public interface IRefreshTokenService
    {
        Task<string> GenerateRefreshToken(string userId);
        Task<(string? accesToken, RefreshToken? newRefreshToken)> GenerateAccessToken(string token);
    }
}
