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
        private readonly IEncryptionHelper _encryptionHelper;

        public ChatMessageRepository(AppDbContext context, IEncryptionHelper encryptionHelper) : base(context)
        {
            _encryptionHelper = encryptionHelper;
        }

        public async Task<List<ChatMessage>> GetMessagesByChatId(int chatId)
        {
            return await _dbSet.Where(x => x.UserChatId == chatId).ToListAsync();
        }

        public async Task<string> GetLastMessageByChatId(int chatId)
        {
            return _encryptionHelper.Decrypt( await _dbSet.Where(x => x.UserChatId == chatId).OrderBy(x => x.SentAt)
                .Select(x => x.Content).LastAsync());
        }
    }
}
