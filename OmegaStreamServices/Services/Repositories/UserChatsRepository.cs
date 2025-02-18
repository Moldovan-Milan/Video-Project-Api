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
    public class UserChatsRepository : BaseRepository<UserChats>, IUserChatsRepository
    {
        public UserChatsRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<UserChats>> GetAllChatByUserIdAsync(string userId)
        {
            return await _dbSet.Include(x => x.User1).Include(x => x.User2) 
                .Where(x => x.User1Id == userId || x.User2Id == userId)
                .ToListAsync();
        }
    }
}
