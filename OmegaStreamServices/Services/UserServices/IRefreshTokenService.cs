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
        Task<string> GetOrGenerateRefreshToken(string userId);
        Task<(bool IsValid, RefreshToken? Token)> ValidateRefreshTokenAsync(string refreshToken);
    }
}
