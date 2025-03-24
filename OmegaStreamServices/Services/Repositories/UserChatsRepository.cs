using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;

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

        public async Task DeleteAllMessages(int chatId)
        {
            var messages = await _context.ChatMessages.Where(x => x.UserChatId == chatId).ToListAsync();
            _context.ChatMessages.RemoveRange(messages);
            await _context.SaveChangesAsync();
        }
    }
}
