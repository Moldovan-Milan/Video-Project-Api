using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using OmegaStreamServices.Models;

namespace OmegaStreamWebAPI.Middlewares
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        private static readonly List<WebSocket> Connections = new();
        private static readonly List<Room> Rooms = new();

        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var ws = await context.WebSockets.AcceptWebSocketAsync();
                Connections.Add(ws);
                await Broadcast("Somebody joined", Connections);
                await ReceiveMessage(ws);
            }
            else
            {
                await _next(context); // Ha nem WebSocket kérés, akkor tovább engedjük a requestet
            }
        }

        private static async Task Broadcast(string message, List<WebSocket> webSockets)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            foreach (var ws in webSockets.Where(ws => ws.State == WebSocketState.Open))
            {
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }

        private static async Task ReceiveMessage(WebSocket ws)
        {
            var buffer = new byte[1024 * 4];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                await HandleMessage(result, buffer, ws);
            }
        }

        private static async Task HandleMessage(WebSocketReceiveResult result, byte[] buffer, WebSocket ws)
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var jsonMessage = JsonSerializer.Deserialize<JsonElement>(message);
                string type = jsonMessage.GetProperty("type").GetString()!;

                switch (type)
                {
                    case "CREATE_ROOM":
                        await CreateRoom(ws);
                        break;
                    case "JOIN_ROOM":
                        string roomId = jsonMessage.GetProperty("roomId").GetString()!;
                        await JoinRoom(roomId, ws);
                        break;
                    case "ROOM_MESSAGE":
                        roomId = jsonMessage.GetProperty("roomId").GetString()!;
                        string roomMessage = jsonMessage.GetProperty("message").GetString()!;
                        await RoomMessage(roomId, roomMessage);
                        break;
                }
            }
            else if (result.MessageType == WebSocketMessageType.Close)
            {
                Connections.Remove(ws);
                await Broadcast("Somebody left the room", Connections);
                if (result.CloseStatus.HasValue)
                {
                    await ws.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }
        }

        private static async Task CreateRoom(WebSocket ws)
        {
            string roomId = Guid.NewGuid().ToString();
            Room room = new() { RoomId = roomId, Connections = new List<WebSocket>() };
            Rooms.Add(room);
            await Broadcast($"Room created! Room ID: {roomId}", new List<WebSocket> { ws });
        }

        private static async Task JoinRoom(string roomId, WebSocket ws)
        {
            var room = Rooms.FirstOrDefault(x => x.RoomId == roomId);
            if (room != null)
            {
                room.Connections.Add(ws);
                await Broadcast($"Somebody joined room {roomId}", room.Connections);
            }
        }

        private static async Task RoomMessage(string roomId, string message)
        {
            var room = Rooms.FirstOrDefault(x => x.RoomId == roomId);
            if (room != null)
            {
                await Broadcast(message, room.Connections);
            }
        }
    }
}
