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

        public async Task<(bool, UserChats? userChat)> HasUserChat(string user1Id, string user2Id)
        {
            UserChats? userChat = await _dbSet.Where(x => x.User1Id == user1Id && x.User2Id == user2Id
            || x.User2Id == user1Id && x.User1Id == user2Id).FirstOrDefaultAsync();
            return (userChat != null, userChat);
        }
    }
}
