using System.IdentityModel.Tokens.Jwt;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;

namespace OmegaStreamWebAPI.WebSockets
{
    public class ChatWebsocketHandler
    {
        private readonly Dictionary<string, WebSocket> _userSockets = new();
        private readonly IServiceScopeFactory _scopeFactory;

        public ChatWebsocketHandler(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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
            var buffer = new byte[4096];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var initMessage = JsonConvert.DeserializeObject<WebSocketMessage>(receivedJson);

            if (initMessage?.Type == "connect" && !string.IsNullOrEmpty(initMessage.Content))
            {
                var userId = GetIdFromToken(initMessage.Content);
                if (!string.IsNullOrEmpty(userId) && !_userSockets.ContainsKey(userId))
                {
                    _userSockets[userId] = webSocket;
                    Console.WriteLine($"Felhasználó csatlakozott: {userId}");
                    await SendResponseAsync(webSocket, "debug", "Connected to webSocket");
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
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
        }

        private async Task ReceiveMessagesAsync(WebSocket webSocket)
        {
            var buffer = new byte[4096];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var message = JsonConvert.DeserializeObject<WebSocketMessage>(receivedJson);
                    if (message != null)
                    {
                        await ProcessMessageAsync(webSocket, message);
                    }
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }

        private async Task ProcessMessageAsync(WebSocket webSocket, WebSocketMessage message)
        {
            using (var scope = _scopeFactory.CreateScope()) // Scoped szolgáltatás létrehozása
            {
                var userChatsRepository = scope.ServiceProvider.GetRequiredService<IUserChatsRepository>();
                var chatMessageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();

                if (message.Type == "message" && !string.IsNullOrEmpty(message.ChatId) && !string.IsNullOrEmpty(message.Content))
                {
                    var chat = await userChatsRepository.FindByIdAsync(int.Parse(message.ChatId));
                    if (chat != null)
                    {
                        string recipientId = chat.User1Id == message.SenderId ? chat.User2Id : chat.User1Id;
                        if (_userSockets.TryGetValue(recipientId, out WebSocket? recipientSocket))
                        {
                            await SendResponseAsync(recipientSocket, "message", message.Content);

                            ChatMessage chatMessage = new ChatMessage
                            {
                                SenderId = message.SenderId,
                                Content = message.Content,
                                UserChatId = chat.Id,
                                SentAt = DateTime.UtcNow
                            };
                            await chatMessageRepository.Add(chatMessage);
                        }
                    }
                }
                else if (message.Type == "get_history" && !string.IsNullOrEmpty(message.ChatId))
                {
                    string history = await GetHistoryJson(int.Parse(message.ChatId));
                    await SendResponseAsync(webSocket, "history", history);
                }
            }
        }

        private async Task SendResponseAsync(WebSocket webSocket, string type, string content)
        {
            var response = JsonConvert.SerializeObject(new WebSocketMessage { Type = type, Content = content });
            var bytes = Encoding.UTF8.GetBytes(response);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private async Task<string> GetHistoryJson(int chatId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var chatMessageRepository = scope.ServiceProvider.GetRequiredService<IChatMessageRepository>();
                List<ChatMessage> chatMessage = await chatMessageRepository.GetMessagesByChatId(chatId);
                return JsonConvert.SerializeObject(chatMessage);
            }
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

        // 30 másodpercenként végignézi, hogy melyik kapcsolat nem él már
        // Ha nem él, akkor eltávolítja
        private async Task MonitorConnectionsAsync()
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));

                foreach (var (userId, socket) in _userSockets.ToList())
                {
                    if (socket.State != WebSocketState.Open)
                    {
                        Console.WriteLine($"Felhasználó inaktív, törlés: {userId}");
                        _userSockets.Remove(userId);
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
