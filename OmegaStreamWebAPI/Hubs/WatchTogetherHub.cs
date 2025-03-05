using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Query;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using System.Collections.Concurrent;

namespace OmegaStreamWebAPI.Hubs
{
    public class WatchTogetherHub : Hub
    {
        // Ebben tároljuk el a szoba aktuális állapotát
        private static ConcurrentDictionary<string, RoomState> RoomStates = new();
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public WatchTogetherHub(UserManager<User> userManager, IMapper mapper)
        {
            _userManager = userManager;
            _mapper = mapper;
        }

        public async Task JoinRoom(string roomId, string userId)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                roomState = new RoomState { Host = user, IsHostInRoom = true };
                RoomStates[roomId] = roomState;
                await Clients.Caller.SendAsync("YouAreHost");
            }
            else if (userId == roomState.Host.Id && !roomState.IsHostInRoom)
            {
                roomState.IsHostInRoom = true;
                await Clients.Caller.SendAsync("YouAreHost");
                await Clients.OthersInGroup(roomId).SendAsync("HostInRoom");
            }

            lock (roomState)
            {
                if (!roomState.Members.Contains(user))
                {
                    roomState.Members.Add(user);
                }
            }

            // Csak az új belépő kapja meg a mentett videóállapotot!
            double correctedTime = roomState.VideoState.CurrentTime;
            if (roomState.VideoState.IsPlaying)
            {
                correctedTime += (DateTime.UtcNow - roomState.VideoState.LastUpdated).TotalSeconds;
            }

            // Csak az új belépőnek küldjük el!
            await Clients.Caller.SendAsync("SyncVideoState", correctedTime, roomState.VideoState.IsPlaying);
            await Clients.Groups(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
        }

        public async Task LeaveRoom(string roomId, string userId)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);

            lock (roomState)
            {
                roomState.Members.RemoveAll(x => x.Id == user.Id);

                if (roomState.Host.Id == user.Id)
                {
                    roomState.IsHostInRoom = false;
                    Clients.OthersInGroup(roomId).SendAsync("HostLeftRoom");
                }
            }

            if (roomState.Members.Count == 0 && !roomState.IsHostInRoom)
            {
                RoomStates.TryRemove(roomId, out _);
            }

            await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(roomState.Members));
        }


        public async Task Play(string roomId, double currentTime)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState)) return;

            lock (roomState)
            {
                roomState.VideoState.CurrentTime = currentTime;
                roomState.VideoState.IsPlaying = true;
                roomState.VideoState.LastUpdated = DateTime.UtcNow;
            }

            await Clients.OthersInGroup(roomId).SendAsync("ReceivePlay", currentTime);
        }


        public async Task Pause(string roomId, double currentTime)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState)) return;

            lock (roomState)
            {
                roomState.VideoState = new VideoState
                {
                    CurrentTime = currentTime,
                    IsPlaying = false,
                    LastUpdated = DateTime.UtcNow
                };
            }
            await Clients.OthersInGroup(roomId).SendAsync("ReceivePause", currentTime);
        }

        public async Task Seek(string roomId, double currentTime)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState)) return;

            lock (roomState)
            {
                roomState.VideoState = new VideoState
                {
                    CurrentTime = currentTime,
                    LastUpdated = DateTime.UtcNow,
                    IsPlaying = true
                };
            }

            await Clients.OthersInGroup(roomId).SendAsync("ReceiveSeek", currentTime);
        }

        // A host elküldi a saját idejét
        public async Task SyncTime(string roomId, double hostTime)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState)) return;

            roomState.VideoState.CurrentTime = hostTime;
            await Clients.OthersInGroup(roomId).SendAsync("HostTimeSync", hostTime);

        }

        private class VideoState
        {
            public double CurrentTime { get; set; }
            public bool IsPlaying { get; set; }
            public DateTime LastUpdated { get; set; }
        }

        private class RoomState
        {
            public User Host { get; set; } = new();
            public bool IsHostInRoom { get; set; } = false;
            public List<User> Members { get; set; } = new();
            public VideoState VideoState { get; set; } = new();
        }
    }
}
