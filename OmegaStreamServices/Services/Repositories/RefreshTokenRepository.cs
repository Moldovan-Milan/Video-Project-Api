using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
    {
        private static readonly List<RefreshToken> refreshTokens = new();

        public RefreshTokenRepository()
        {
        }

        public override Task Add(RefreshToken entity)
        {
            refreshTokens.Add(entity);
            return Task.CompletedTask;
        }

        public override void Delete(RefreshToken entity)
        {
            refreshTokens.Remove(entity);
        }

        public override async Task<RefreshToken> FindByIdAsync(int id)
        {
            return await Task.FromResult(refreshTokens.FirstOrDefault(x => x.Id == id));
        }

        public override async Task<List<RefreshToken>> GetAll()
        {
            return await Task.FromResult(refreshTokens.ToList());
        }

        public async Task<RefreshToken> GetByToken(string token)
        {
            return await Task.FromResult(refreshTokens.FirstOrDefault(x => x.Token == token))!;
        }

        public async Task<RefreshToken> GetByUserId(string userId)
        {
            return await Task.FromResult(refreshTokens.FirstOrDefault(x => x.UserId == userId))!;
        }

        public Task<User?> GetUserByToken(string token)
        {
            return null; 
        }

        public override void Update(RefreshToken entity)
        {
            int id = refreshTokens.IndexOf(refreshTokens.Find(x => x.Id == entity.Id));
            refreshTokens[id] = entity;
        }

        Task<string?> IRefreshTokenRepository.GetUserByToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}
