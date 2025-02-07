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
        public RefreshTokenRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<RefreshToken> GetByToken(string token)
        {
            return await _dbSet.Include(x => x.User).FirstOrDefaultAsync(x => x.Token == token)!;
        }

        public async Task<RefreshToken> GetByUserId(string userId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.UserId == userId)!;
        }
    }
}
