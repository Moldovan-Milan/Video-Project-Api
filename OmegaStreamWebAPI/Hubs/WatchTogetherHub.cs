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

        private const int MAX_USER_COUNT_IN_ROOM = 8;

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


            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                roomState = new RoomState { Host = user, IsHostInRoom = true,
                HostConnId = Context.ConnectionId};
                RoomStates[roomId] = roomState;
                roomState.Members.Add(user);

                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

                await Clients.Caller.SendAsync("YouAreHost");
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
            else if (userId == roomState.Host.Id && !roomState.IsHostInRoom)
            {
                roomState.IsHostInRoom = true;
                await Clients.Caller.SendAsync("YouAreHost");
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);


                roomState.Members.Add(user);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));


                await Clients.OthersInGroup(roomId).SendAsync("HostInRoom");
            }
            // Van szoba, kérést küld a host felé
            else
            {
                if (roomState.Members.Count < MAX_USER_COUNT_IN_ROOM)
                {
                    roomState.WaitingForAccept[user.Id] = Context.ConnectionId;
                    await Clients.Clients(roomState.HostConnId).SendAsync("JoinRequest", _mapper.Map<UserDto>(user));
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Room is full");
                }
                
            }
        }

        public async Task AcceptUser(string roomId, string userId)
        {
            if (!RoomStates.TryGetValue(roomId, out var roomState))
            {
                await Clients.Caller.SendAsync("Error", "Room not found");
                return;
            }

            if (!roomState.WaitingForAccept.TryGetValue(userId, out string acceptedUserConnId))
            {
                await Clients.Caller.SendAsync("Error", "User not found in waiting list");
                return;
            }

            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await Clients.Caller.SendAsync("Error", "User not found");
                return;
            }

            roomState.WaitingForAccept.Remove(userId);
            roomState.UserIdAndConnId[userId] = acceptedUserConnId;
            roomState.Members.Add(user);

            double correctedTime = roomState.VideoState.CurrentTime;
            if (roomState.VideoState.IsPlaying)
            {
                correctedTime += (DateTime.UtcNow - roomState.VideoState.LastUpdated).TotalSeconds;
            }

            await Groups.AddToGroupAsync(acceptedUserConnId, roomId);
            await Clients.Client(acceptedUserConnId).SendAsync("RequestAccepted", correctedTime, roomState.VideoState.IsPlaying);
            await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
        }

        public async Task RejectUser(string roomId, string userId)
        {
            if (RoomStates.TryGetValue(roomId, out var roomState))
            {
                await Clients.Client(roomState.WaitingForAccept[userId]).SendAsync("RequestRejected");
                roomState.WaitingForAccept.Remove(userId);

            }
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
                roomState.Members.RemoveAll(x => x.Id == userId);

                if (roomState.Host.Id == user.Id)
                {
                    roomState.IsHostInRoom = false;
                    roomState.HostConnId = string.Empty; // Host kapcsolatának törlése
                    _ = Clients.OthersInGroup(roomId).SendAsync("HostLeftRoom");
                }
            }

            if (roomState.Members.Count == 0 && !roomState.IsHostInRoom)
            {
                RoomStates.TryRemove(roomId, out _);
            }
            else
            {
                await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
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
            if (!RoomStates.TryGetValue(roomId, out var roomState) || roomState.HostConnId != Context.ConnectionId)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            lock (roomState)
            {
                roomState.VideoState.CurrentTime = hostTime;
                roomState.VideoState.LastUpdated = DateTime.UtcNow;
            }

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
            public string HostConnId { get; set; } = string.Empty;
            public bool IsHostInRoom { get; set; } = false;
            public List<User> Members { get; set; } = new();
            public Dictionary<string, string> UserIdAndConnId { get; set; } = new();
            public Dictionary<string, string> WaitingForAccept { get; set; } = new();
            public VideoState VideoState { get; set; } = new VideoState();
        }

    }
}
