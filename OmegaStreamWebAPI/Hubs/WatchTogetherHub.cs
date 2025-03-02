using Microsoft.AspNetCore.SignalR;

namespace OmegaStreamWebAPI.Hubs
{
    public class WatchTogetherHub: Hub
    {
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("JoinedToRoom");
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("LeavedRoom");
        }

        public async Task Play(string roomId, double currentTime)
        {
            await Clients.OthersInGroup(roomId).SendAsync("ReceivePlay", currentTime);
        }

        public async Task Pause(string roomId, double currentTime)
        {
            await Clients.OthersInGroup(roomId).SendAsync("ReceivePause", currentTime);
        }

        public async Task Seek(string roomId, double currentTime)
        {
            await Clients.OthersInGroup(roomId).SendAsync("ReceiveSeek", currentTime);
        }
    }
}
