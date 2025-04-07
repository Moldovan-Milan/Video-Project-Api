using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IUserChatsRepository : IBaseRepository<UserChats>
    {
        Task<List<UserChats>> GetAllChatByUserIdAsync(string userId);
        Task DeleteAllMessages(int chatId);
        Task<(bool, UserChats? userChat)> HasUserChat(string user1Id, string user2Id);
    }
}
