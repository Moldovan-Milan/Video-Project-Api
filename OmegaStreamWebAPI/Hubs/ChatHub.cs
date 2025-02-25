using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Hubs
{
    public class ChatHub: Hub
    {
        private readonly IUserChatsRepository _userChatsRepository;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IEncryptionHelper _encryptionHelper;

        public ChatHub(IUserChatsRepository userChatsRepository, IChatMessageRepository chatMessageRepository, IEncryptionHelper encryptionHelper)
        {
            _userChatsRepository = userChatsRepository;
            _chatMessageRepository = chatMessageRepository;
            _encryptionHelper = encryptionHelper;
        }

        // Ezt a függvényt a frontend fogja meghívni
        public async Task SendMessage(int chatId, string content)
        {
            var senderId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(content))
            {
                return;
            }

            var chat = await _userChatsRepository.FindByIdAsync(chatId);
            if (chat == null)
                return;

            string recipientId = chat.User1Id == senderId ? chat.User2Id : chat.User1Id;

            ChatMessage chatMessage = new ChatMessage
            {
                UserChatId = chat.Id,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.Now
            };

            string jsonMessage = JsonConvert.SerializeObject(chatMessage, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            // Üzenet küldése a chat résztvevőinek
            await Clients.User(senderId).SendAsync("ReceiveMessage", jsonMessage);
            await Clients.Users(recipientId).SendAsync("ReceiveMessage", jsonMessage);

            chatMessage.Content = _encryptionHelper.Encrypt(content);
            await _chatMessageRepository.Add(chatMessage);
        }


        public async Task RequestChatHistory(int chatId)
        {

            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("User is not authenticated.");
            }

            var chat = await _userChatsRepository.FindByIdAsync(chatId);
            if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
            {
                throw new HubException("Access denied: You are not a participant of this chat.");
            }

            var chatMessages = await _chatMessageRepository.GetMessagesByChatId(chatId);
            if (chatMessages == null || !chatMessages.Any())
            {
                throw new HubException("No messages found for this chat.");
            }

            chatMessages = chatMessages.Select(msg =>
            {
                msg.Content = _encryptionHelper.Decrypt(msg.Content);
                return msg;
            }).ToList();

            await Clients.Caller.SendAsync("ReceiveChatHistory", chatMessages);
        }

    }
}
