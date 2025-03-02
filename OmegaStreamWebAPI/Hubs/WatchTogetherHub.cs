using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace OmegaStreamWebAPI.Hubs
{
    public class WatchTogetherHub: Hub
    {
        // Ebben tároljuk el a szoba aktuális állapotát
        private static ConcurrentDictionary<string, PlayListState> RoomStates = new();

        public async Task JoinRoom(string roomId)
        {

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            if (RoomStates.TryGetValue(roomId, out var videoState))
            {
                double correctedTime = videoState.CurrentTime;
                if (videoState.IsPlaying)
                {
                    // Ha a videó megy, kiszámoljuk az aktuális idejét
                    var elapsed = (DateTime.UtcNow - videoState.LastUpdated).TotalSeconds;
                    correctedTime += elapsed;
                    await Clients.Caller.SendAsync("SyncVideoState",
                    correctedTime, videoState.IsPlaying);
                }
                // Ha meg van állítva, akkor nem kell számolni
                else
                {
                    await Clients.Caller.SendAsync("SyncVideoState",
                    videoState.CurrentTime, videoState.IsPlaying);
                }
            }

            await Clients.Caller.SendAsync("JoinedToRoom");
        }

        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Caller.SendAsync("LeavedRoom");
        }

        public async Task Play(string roomId, double currentTime)
        {
            RoomStates[roomId] = new PlayListState
            {
                CurrentTime = currentTime,
                IsPlaying = true,
                LastUpdated = DateTime.UtcNow
            };

            await Clients.OthersInGroup(roomId).SendAsync("ReceivePlay", currentTime);
        }

        public async Task Pause(string roomId, double currentTime)
        {
            RoomStates[roomId] = new PlayListState
            {
                CurrentTime = currentTime,
                IsPlaying = false,
                LastUpdated = DateTime.UtcNow
            };

            await Clients.OthersInGroup(roomId).SendAsync("ReceivePause", currentTime);
        }

        public async Task Seek(string roomId, double currentTime)
        {
            if (RoomStates.ContainsKey(roomId))
            {
                RoomStates[roomId].CurrentTime = currentTime;
                RoomStates[roomId].LastUpdated = DateTime.UtcNow;
            }

            await Clients.OthersInGroup(roomId).SendAsync("ReceiveSeek", currentTime);
        }

        private class PlayListState
        {

            public double CurrentTime { get; set; }
            public bool IsPlaying { get; set; }
            public DateTime LastUpdated { get; set; }
        }
    }
}
