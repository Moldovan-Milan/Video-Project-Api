using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.UserServices
{
    public class RefreshTokenService: IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEncryptionHelper _encryptionHelper;

        public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, IEncryptionHelper encryptionHelper)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _encryptionHelper = encryptionHelper;
        }

        public async Task<string> GetOrGenerateRefreshToken(string userId)
        {
            var refreshToken = await _refreshTokenRepository.GetByUserId(userId);
            if (refreshToken == null)
            {
                refreshToken = await TokenGenerator.GenerateRefreshToken(userId);
                refreshToken.Token = _encryptionHelper.Encrypt(refreshToken.Token);
                await _refreshTokenRepository.Add(refreshToken);
            }
            return _encryptionHelper.Decrypt(refreshToken.Token);
        }

        public async Task<(bool IsValid, RefreshToken? Token)> ValidateRefreshTokenAsync(string refreshToken)
        {
            string encryptedToken = _encryptionHelper.Encrypt(refreshToken);
            var validToken = await _refreshTokenRepository.GetByToken(encryptedToken);

            if (validToken == null || validToken.ExpiryDate <= DateTime.UtcNow)
            {
                if (validToken != null)
                    _refreshTokenRepository.Delete(validToken);
                return (false, null);
            }

            return (true, validToken);
        }

    }
}
