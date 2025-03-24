using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IUserChatsRepository : IBaseRepository<UserChats>
    {
        Task<List<UserChats>> GetAllChatByUserIdAsync(string userId);
        Task DeleteAllMessages(int chatId);
    }
}
