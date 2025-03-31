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
        private readonly TokenGenerator _tokenGenerator;

        public RefreshTokenService(IRefreshTokenRepository refreshTokenRepository, IEncryptionHelper encryptionHelper, TokenGenerator tokenGenerator)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _encryptionHelper = encryptionHelper;
            _tokenGenerator = tokenGenerator;
        }

        public async Task<(string? accesToken, RefreshToken? newRefreshToken)> GenerateAccessToken(string token)
        {
            var oldRefreshToken = await _refreshTokenRepository.GetByToken(token);
            if (oldRefreshToken == null)
                return (null, null);
            if (ValidateRefreshToken(oldRefreshToken))
                return (null, null);

            RefreshToken newRefreshToken = await _tokenGenerator.GenerateRefreshToken(oldRefreshToken.UserId);

            _refreshTokenRepository.Delete(oldRefreshToken);
            await _refreshTokenRepository.Add(newRefreshToken);

            string accessToken = await _tokenGenerator.GenerateJwtToken(newRefreshToken.UserId);
            return (accessToken, newRefreshToken);
        }

        public async Task<string> GenerateRefreshToken(string userId)
        {
            var refreshToken = await _refreshTokenRepository.GetByUserId(userId);
            if (refreshToken == null)
            {
                refreshToken = await _tokenGenerator.GenerateRefreshToken(userId);
                await _refreshTokenRepository.Add(refreshToken);
            }
            return refreshToken.Token;
        }

        private bool ValidateRefreshToken(RefreshToken refreshToken)
        {
            return refreshToken.ExpiryDate <= DateTime.UtcNow;
        }

        //public async Task<string> GetOrGenerateRefreshToken(string userId)
        //{
        //    var refreshToken = await _refreshTokenRepository.GetByUserId(userId);
        //    if (refreshToken == null)
        //    {
        //        refreshToken = TokenGenerator.GenerateRefreshToken(userId);
        //        //refreshToken.Token = _encryptionHelper.Encrypt(refreshToken.Token);
        //        await _refreshTokenRepository.Add(refreshToken);
        //    }
        //    return refreshToken.Token;
        //}

        //public async Task<(bool IsValid, RefreshToken? Token)> ValidateRefreshTokenAsync(string refreshToken)
        //{
        //    //string encryptedToken = _encryptionHelper.Encrypt(refreshToken);
        //    var validToken = await _refreshTokenRepository.GetByToken(refreshToken);

        //    if (validToken == null || validToken.ExpiryDate <= DateTime.UtcNow)
        //    {
        //        if (validToken != null)
        //            _refreshTokenRepository.Delete(validToken);
        //        return (false, null);
        //    }
        //    _refreshTokenRepository.Delete(validToken);
        //    var newRefreshToken = TokenGenerator.GenerateRefreshToken(validToken.UserId);

        //    return (true, newRefreshToken);
        //}
    }
}
