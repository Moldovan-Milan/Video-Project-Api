using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Hubs
{
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _connectedUsers = new ConcurrentDictionary<string, string>();

        private readonly IGenericRepository _repo;
        //private readonly IUserChatsRepository _userChatsRepository;
        //private readonly IChatMessageRepository _chatMessageRepository;
        private readonly IEncryptionHelper _encryptionHelper;

        public ChatHub(IEncryptionHelper encryptionHelper, IGenericRepository repo)
        {
            //_userChatsRepository = userChatsRepository;
            //_chatMessageRepository = chatMessageRepository;
            _encryptionHelper = encryptionHelper;
            _repo = repo;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                if (_connectedUsers.ContainsKey(userId))
                {
                    _connectedUsers.TryRemove(userId, out _);
                }

                _connectedUsers.TryAdd(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _connectedUsers.TryRemove(userId, out _);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(int chatId, string content)
        {
            var senderId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(content))
            {
                return;
            }
            var chat = await _repo.FirstOrDefaultAsync<UserChats>(c => c.Id == chatId);
            if (chat == null)
                return;

            string recipientId = chat.User1Id == senderId ? chat.User2Id : chat.User1Id;

            ChatMessage chatMessage = new ChatMessage
            {
                UserChatId = chat.Id,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow
            };

            string jsonMessage = JsonConvert.SerializeObject(chatMessage, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });

            if (_connectedUsers.TryGetValue(senderId, out string senderConnectionId))
            {
                await Clients.Client(senderConnectionId).SendAsync("ReceiveMessage", jsonMessage);
            }

            if (_connectedUsers.TryGetValue(recipientId, out string recipientConnectionId))
            {
                await Clients.Client(recipientConnectionId).SendAsync("ReceiveMessage", jsonMessage);
            }

            chatMessage.Content = _encryptionHelper.Encrypt(content);
            await _repo.AddAsync(chatMessage);
        }

        public async Task RequestChatHistory(int chatId)
        {
            var userId = Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("User is not authenticated.");
            }

            var chat = await _repo.FirstOrDefaultAsync<UserChats>(c => c.Id == chatId);
            if (chat == null || (chat.User1Id != userId && chat.User2Id != userId))
            {
                throw new HubException("Access denied: You are not a participant of this chat.");
            }

            var chatMessages = await _repo.GetAllAsync<ChatMessage>(m => m.UserChatId == chatId);
            //if (chatMessages == null || !chatMessages.Any())
            //{
            //    throw new HubException("No messages found for this chat.");
            //}
            

            chatMessages = chatMessages.Select(msg =>
            {
                msg.Content = _encryptionHelper.Decrypt(msg.Content);
                return msg;
            }).ToList();

            if (_connectedUsers.TryGetValue(userId, out string connectionId))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveChatHistory", chatMessages);
            }
        }
    }
}
