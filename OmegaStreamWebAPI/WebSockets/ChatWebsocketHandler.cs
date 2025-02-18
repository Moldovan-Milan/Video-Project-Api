using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;

namespace OmegaStreamWebAPI.WebSockets
{
    public class ChatWebsocketHandler
    {
        private readonly Dictionary<string, WebSocket> _userSockets = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEncryptionHelper _encryptionHelper;
        private const int BufferSize = 4096;

        public ChatWebsocketHandler(IServiceScopeFactory scopeFactory, IEncryptionHelper encryptionHelper)
        {
            _scopeFactory = scopeFactory;
            _encryptionHelper = encryptionHelper;
            Task.Run(MonitorConnectionsAsync); // Háttérfolyamat indítása
        }

        public async Task HandleWebsocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var initMessage = await ReceiveMessageAsync<WebSocketMessage>(webSocket);

            if (initMessage?.Type == "connect" && !string.IsNullOrEmpty(initMessage.Content))
            {
                var userId = GetIdFromToken(initMessage.Content);
                if (!string.IsNullOrEmpty(userId) && !_userSockets.ContainsKey(userId))
                {
                    _userSockets[userId] = webSocket;
                    Console.WriteLine($"Felhasználó csatlakozott: {userId}");
                    await SendMessageAsync(webSocket, "debug", "Connected to WebSocket");
                }
            }

            try
            {
                await ReceiveMessagesAsync(webSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                await CloseAndRemoveSocketAsync(webSocket);
            }
        }

        private async Task ReceiveMessagesAsync(WebSocket webSocket)
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var message = await ReceiveMessageAsync<WebSocketMessage>(webSocket);
                if (message != null)
                {
                    await ProcessMessageAsync(webSocket, message);
                }
            }
        }

        private async Task ProcessMessageAsync(WebSocket webSocket, WebSocketMessage message)
        {
            using var scope = _scopeFactory.CreateScope();
            var userChatsRepository = scope.ServiceProvider.GetRequiredService<IUserChatsRepository>();
            var chatMessageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();

            switch (message.Type)
            {
                case "message":
                    if (IsValidMessage(message))
                    {
                        await HandleChatMessageAsync(message, userChatsRepository, chatMessageRepository);
                    }
                    else
                    {
                        await SendMessageAsync(webSocket, "error", "Not valid content");
                    }
                        break;
                case "get_history":
                    await SendChatHistoryAsync(webSocket, message.ChatId, chatMessageRepository);
                    break;
            }
        }

        private async Task HandleChatMessageAsync(
            WebSocketMessage message,
            IUserChatsRepository userChatsRepository,
            IChatMessageRepository chatMessageRepository)
        {
            if (string.IsNullOrEmpty(message.ChatId) || string.IsNullOrEmpty(message.Content))
                return;

            var chat = await userChatsRepository.FindByIdAsync(int.Parse(message.ChatId));
            if (chat == null) return;

            string recipientId = chat.User1Id == message.SenderId ? chat.User2Id : chat.User1Id;

            var chatMessage = new ChatMessage
            {
                SenderId = message.SenderId,
                Content = message.Content,
                UserChatId = chat.Id,
                SentAt = DateTime.UtcNow
            };

            string jsonMessage = JsonConvert.SerializeObject(chatMessage);

            if (_userSockets.TryGetValue(message.SenderId, out var senderSocket))
                await SendMessageAsync(senderSocket, "message", jsonMessage);

            if (_userSockets.TryGetValue(recipientId, out var recipientSocket))
                await SendMessageAsync(recipientSocket, "message", jsonMessage);

            chatMessage.Content = _encryptionHelper.Encrypt(chatMessage.Content);
            await chatMessageRepository.Add(chatMessage);
        }

        private async Task SendChatHistoryAsync(WebSocket webSocket, string? chatId, IChatMessageRepository chatMessageRepository)
        {
            if (string.IsNullOrEmpty(chatId))
                return;

            var chatMessages = await chatMessageRepository.GetMessagesByChatId(int.Parse(chatId));
            chatMessages = DecryptMessages(chatMessages);

            string historyJson = JsonConvert.SerializeObject(chatMessages);
            await SendMessageAsync(webSocket, "history", historyJson);
        }

        private async Task<T?> ReceiveMessageAsync<T>(WebSocket webSocket)
        {
            var buffer = new byte[BufferSize];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
                return default;

            string receivedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            return JsonConvert.DeserializeObject<T>(receivedJson);
        }

        private async Task SendMessageAsync(WebSocket webSocket, string type, string content)
        {
            var response = JsonConvert.SerializeObject(new WebSocketMessage { Type = type, Content = content });
            var bytes = Encoding.UTF8.GetBytes(response);

            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task CloseAndRemoveSocketAsync(WebSocket webSocket)
        {
            var userId = _userSockets.FirstOrDefault(x => x.Value == webSocket).Key;
            if (!string.IsNullOrEmpty(userId))
            {
                _userSockets.Remove(userId);
                Console.WriteLine($"Felhasználó kilépett: {userId}");
            }

            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
        }

        private string? GetIdFromToken(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(token) as JwtSecurityToken;
            var userId = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;
            var exp = jsonToken?.Claims.FirstOrDefault(claim => claim.Type == "exp")?.Value;

            return exp != null && long.TryParse(exp, out long expSeconds) && DateTimeOffset.FromUnixTimeSeconds(expSeconds) > DateTime.UtcNow
                ? userId
                : null;
        }

        private List<ChatMessage> DecryptMessages(List<ChatMessage> chatMessages)
        {
            foreach (var message in chatMessages)
            {
                message.Content = _encryptionHelper.Decrypt(message.Content);
            }
            return chatMessages;
        }

        private bool IsValidMessage(WebSocketMessage message)
        {
            return !string.IsNullOrEmpty(message.Type) &&
                   message.Type.All(char.IsLetterOrDigit) &&
                   !string.IsNullOrEmpty(message.Content) &&
                   message.Content.Length <= 2000; // Limitáljuk az üzenetek méretét
        }

        private async Task MonitorConnectionsAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(10));

                foreach (var (userId, socket) in _userSockets.ToList())
                {
                    if (socket.State != WebSocketState.Open)
                    {
                        Console.WriteLine($"Felhasználó inaktív, törlés: {userId}");
                        _userSockets.Remove(userId);
                        await CloseAndRemoveSocketAsync(socket);
                    }
                    else
                    {
                        try
                        {
                           await socket.SendAsync(Encoding.UTF8.GetBytes("ping"), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Kapcsolati hiba. ${userId} eltávolítása");
                            _userSockets.Remove(userId);
                        }
                    }
                }
            }
        }
    }

    public class WebSocketMessage
    {
        public string Type { get; set; } = "";
        public string? Content { get; set; }
        public string? ChatId { get; set; }
        public string? SenderId { get; set; }
    }
}
