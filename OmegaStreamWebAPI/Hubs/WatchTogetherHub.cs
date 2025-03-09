using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.UserServices;
using System.Collections.Concurrent;

namespace OmegaStreamWebAPI.Hubs
{
    public class WatchTogetherHub : Hub
    {
        private readonly UserManager<User> _userManager;
        private readonly IRoomStateManager _roomManager;
        private readonly IMapper _mapper;

        public WatchTogetherHub(UserManager<User> userManager, IRoomStateManager roomManager, IMapper mapper)
        {
            _userManager = userManager;
            _roomManager = roomManager;
            _mapper = mapper;
        }

        public async Task JoinRoom(string roomId, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErroMessage(Context.ConnectionId, "User not found");
                return;
            }

            RoomStateResult result = await _roomManager.AddUserToRoom(roomId, user, Context.ConnectionId, out var roomState);
            
            if (result == RoomStateResult.Created)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                await Clients.Caller.SendAsync("YouAreHost", null);
            }
            else if (result == RoomStateResult.HostReconected && roomState != null)
            {
                var messages = _roomManager.GetHistory(roomId);
                await Clients.Caller.SendAsync("YouAreHost", messages);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                await Clients.OthersInGroup(roomId).SendAsync("HostInRoom");
            }
            else if(result == RoomStateResult.Banned)
            {
                await Clients.Caller.SendAsync("YouAreBanned");
            }
            else if (result == RoomStateResult.Accepted && roomState != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
            else if (result == RoomStateResult.NeedsAproval && roomState != null)
            {
                await Clients.Clients(roomState.HostConnId).SendAsync("JoinRequest", _mapper.Map<UserDto>(user));
            }
            else if (result == RoomStateResult.RoomIsFull)
            {
                await SendErroMessage(Context.ConnectionId, "Room is full");
            }
        }

        public async Task AcceptUser(string roomId, string userId)
        {
            User? user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                await SendErroMessage(Context.ConnectionId, "User not found");
                return;
            }
    

            var result = await _roomManager.AcceptUser(roomId, user, out string connId, out RoomState roomState);
            if (result == RoomStateResult.Accepted)
            {
                await Groups.AddToGroupAsync(connId, roomId);

                var messages = _roomManager.GetHistory(roomId);
                await Clients.Client(connId).SendAsync("RequestAccepted", roomState.VideoState.CurrentTime, roomState.VideoState.IsPlaying, messages);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
            else if (result == RoomStateResult.RoomIsFull)
            {
                await SendErroMessage(Context.ConnectionId, "Room is full");
                await Clients.Client(connId).SendAsync("Error", "Room is full");
            }
            else if (result == RoomStateResult.Failed)
            {
                await SendErroMessage(Context.ConnectionId, "Failed to accept");
            }
        }

        public async Task RejectUser(string roomId, string userId)
        {
            var result = await _roomManager.RejectUser(roomId, userId, out string connId, out _);
            if (result)
            {
                await Clients.Client(connId).SendAsync("RequestRejected");
            }
        }

        public async Task LeaveRoom(string roomId, string userId)
        {
            var result = await _roomManager.RemoveUserFromRoom(roomId, userId, Context.ConnectionId, out var roomState);
            if (result && roomState == null)
            {
                await Clients.Group(roomId).SendAsync("RoomClosed");
            }
            else if (result)
            {
                if (!roomState.IsHostInRoom)
                {
                    await Clients.Group(roomId).SendAsync("HostLeftRoom");
                }
                
                await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
        }

        public async Task SyncVideoState(string roomId, double currentTime, bool isPlaying)
        {
            var (isSuccess, syncMessage) = _roomManager.UpdateVideoState(roomId, currentTime, isPlaying);
            if (isSuccess)
            {
                await Clients.OthersInGroup(roomId).SendAsync(syncMessage, currentTime);
            }
        }

        public async Task SyncTime(string roomId, double currentTime)
        {
            var roomState = _roomManager.SyncTime(roomId, currentTime);
            if (roomState != null && roomState.HostConnId == Context.ConnectionId)
            {
                await Clients.OthersInGroup(roomId).SendAsync("HostTimeSync", currentTime);
            }
        }

        public async Task SendMessage(string roomId, string userId, string content)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErroMessage(Context.ConnectionId, "User not found");
                return;
            }

            if (!_roomManager.SaveMessage(roomId, _mapper.Map<UserDto>(user), content, out var message))
            {
                await SendErroMessage(Context.ConnectionId, "Failed to save message");
                return;
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }

        public async Task BanUser(string roomId, string userId)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErroMessage("Error", "User not found");
                return;
            }
            
            if (_roomManager.BanUser(roomId, user.Id, out string connId, out List<User> members))
            {
                await Clients.Client(connId).SendAsync("YouAreBanned");
                await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(members));

            }
        }

        private async Task SendErroMessage(string connectionId, string message) 
        {
            await Clients.Clients(connectionId).SendAsync("Error", message);
        }

    }
}
