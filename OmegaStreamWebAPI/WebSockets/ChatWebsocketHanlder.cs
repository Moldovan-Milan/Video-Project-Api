using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using OmegaStreamServices.Models;

namespace OmegaStreamWebAPI.WebSockets
{
    public class ChatWebsocketHanlder
    {
        private readonly List<WebSocket> _sockets = new();
        private readonly Dictionary<string, WebSocket> _userSockets = new();

        private List<UserChats> userChats = new List<UserChats>
        {
            new UserChats
            {
                Id = 1,
                User1Id = "asd1",
                User2Id = "asd2",
                Created = DateTime.UtcNow.AddDays(1)
            }
        };

        private List<ChatMessage> chatMessages = new List<ChatMessage>
        {
            new ChatMessage
            {
                Id = 1,
                UserChatId = 1,
                Content = "Szia, hogy vagy",
                SenderId = "asd1",
                SentAt = DateTime.UtcNow.AddMinutes(3)
            },
            new ChatMessage
            {
                Id = 1,
                UserChatId = 1,
                Content = "Köszi, jól vagyok",
                SenderId = "asd2",
                SentAt = DateTime.UtcNow.AddMinutes(5)
            }
        };

        public async Task HandleWebsocketAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            //_sockets.Add(webSocket);

            // Megnézzük, hogy a felhasználó először csatlakozik-e
            var buffer = new byte[1024 * 4];
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            var receivedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);

            var initMessage = JsonConvert.DeserializeObject<WebSocketMessage>(receivedJson);
            if (initMessage?.Type == "connect" && !string.IsNullOrEmpty(initMessage.Content))
            {
                string userId = initMessage.Content;
                _userSockets[userId] = webSocket;
                Console.WriteLine($"Felhasználó csatlakozott: {userId}");
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
                _sockets.Remove(webSocket);
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
        }

        private async Task ReceiveMessagesAsync(WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var receivedJson = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    try
                    {
                        var message = JsonConvert.DeserializeObject<WebSocketMessage>(receivedJson);

                        if (message != null)
                        {
                            await ProcessMessageAsync(webSocket, message);
                        }
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON parse error: {ex.Message}");
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
            switch (message.Type)
            {
                case "message":
                    if (!string.IsNullOrEmpty(message.ChatId) && !string.IsNullOrEmpty(message.Content))
                    {
                        // Megkeressük a címzettet
                        var chat = userChats.FirstOrDefault(x => x.Id.ToString() == message.ChatId);
                        if (chat != null)
                        {
                            string recipientId = chat.User1Id == message.SenderId ? chat.User2Id : chat.User1Id;

                            if (_userSockets.TryGetValue(recipientId, out WebSocket? recipientSocket))
                            {
                                await SendResponseAsync(recipientSocket, "message", message.Content);
                                chatMessages.Add(new ChatMessage
                                {
                                    Id = 3,
                                    SenderId = recipientId == chat.User1Id ? chat.User2Id : chat.User1Id,
                                    Content = message.Content,
                                    UserChatId = chat.Id,
                                    SentAt = DateTime.Now
                                });
                            }
                        }
                    }
                    break;

                case "get_history":
                    Console.WriteLine($"Fetching chat history for chatId: {message.ChatId}");
                    int.TryParse(message.ChatId, out int chatId);
                    string response = GetHistoryJson(chatId);
                    await SendResponseAsync(webSocket, "history", response);
                    break;

                default:
                    await SendResponseAsync(webSocket, "error", $"Ismeretlen üzenettípus.");
                    break;
            }
        }

        private async Task SendResponseAsync(WebSocket webSocket, string type, string content)
        {
            var response = new WebSocketMessage
            {
                Type = type,
                Content = content
            };
            var jsonResponse = JsonConvert.SerializeObject(response);
            var bytes = Encoding.UTF8.GetBytes(jsonResponse);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }

        private string GetHistoryJson(int chatId)
        {
            var chats = chatMessages.Where(x => x.UserChatId == chatId).ToList();
            string result = JsonConvert.SerializeObject(chats);
            return result;
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