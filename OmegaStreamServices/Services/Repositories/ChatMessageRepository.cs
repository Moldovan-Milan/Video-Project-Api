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
    public class ChatMessageRepository : BaseRepository<ChatMessage>, IChatMessageRepository
    {
        public ChatMessageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ChatMessage>> GetMessagesByChatId(int chatId)
        {
            return await _dbSet.Where(x => x.UserChatId == chatId).ToListAsync();
        }
    }
}
