using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.UserServices;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Hubs
{
    public class WatchTogetherHub : Hub
    {
        private readonly UserManager<User> _userManager;
        private readonly IVideoRepository _videoRepository;
        private readonly IRoomStateManager _roomManager;
        private readonly IMapper _mapper;

        public WatchTogetherHub(UserManager<User> userManager, IRoomStateManager roomManager, IMapper mapper, IVideoRepository videoRepository)
        {
            _userManager = userManager;
            _roomManager = roomManager;
            _mapper = mapper;
            _videoRepository = videoRepository;
        }

        public async Task JoinRoom(string roomId, string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            RoomStateResult result = _roomManager.AddUserToRoom(roomId, user, Context.ConnectionId, out var roomState);

            switch (result)
            {
                case RoomStateResult.Created:
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                    await Clients.Caller.SendAsync("YouAreHost");
                    break;

                // Egyenlőre nem fut le soha
                case RoomStateResult.HostReconected:
                    if (roomState != null)
                    {
                        await Clients.Caller.SendAsync("YouAreHost", roomState.RoomMessages);
                        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                        await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                        await Clients.OthersInGroup(roomId).SendAsync("HostInRoom");
                    }
                    break;

                case RoomStateResult.Banned:
                    await Clients.Caller.SendAsync("YouAreBanned");
                    break;

                case RoomStateResult.NeedsAproval:
                    if (roomState != null)
                    {
                        await Clients.Clients(roomState.HostConnId).SendAsync("JoinRequest", _mapper.Map<UserDto>(user));
                    }
                    break;

                case RoomStateResult.RoomIsFull:
                    await SendErrorMessage(Context.ConnectionId, "Room is full");
                    break;
                case RoomStateResult.Failed:
                    await Clients.Caller.SendAsync("ConnectionFailed");
                    break;
            }

        }

        public async Task AcceptUser(string roomId, string userId)
        {
            User? user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }
    

            var result = _roomManager.AcceptUser(roomId, user, out string connId, out RoomState roomState);
            if (result == RoomStateResult.Accepted)
            {
                await Groups.AddToGroupAsync(connId, roomId);

                VideoDto? currentVideo = roomState.PlayList.FirstOrDefault(x => x.Id == roomState.CurrentVideoId);

                await Clients.Client(connId).SendAsync("RequestAccepted", roomState.VideoState.CurrentTime, roomState.VideoState.IsPlaying, 
                    roomState.RoomMessages, roomState.PlayList, currentVideo);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
            else if (result == RoomStateResult.RoomIsFull)
            {
                await SendErrorMessage(Context.ConnectionId, "Room is full");
            }
            else if (result == RoomStateResult.Failed)
            {
                await SendErrorMessage(Context.ConnectionId, "Failed to accept");
            }
        }

        public async Task RejectUser(string roomId, string userId)
        {
            var result = _roomManager.RejectUser(roomId, userId, out string connId, out _);
            if (result)
            {
                await Clients.Client(connId).SendAsync("RequestRejected");
            }
        }

        public async Task LeaveRoom(string roomId, string userId)
        {
            RoomStateResult result = _roomManager.RemoveUserFromRoom(roomId, userId, Context.ConnectionId, out var roomState);
            switch (result)
            {
                case RoomStateResult.Accepted:
                    await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                    break;

                // Egyből bezár, ha a host kilép
                case RoomStateResult.HostLeft:
                    //await Clients.Group(roomId).SendAsync("HostLeftRoom");
                    await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                    await Clients.Group(roomId).SendAsync("RoomClosed");
                    break;

                case RoomStateResult.RoomClosed:
                    await Clients.Group(roomId).SendAsync("RoomClosed");
                    break;

                case RoomStateResult.Failed:
                    await SendErrorMessage(Context.ConnectionId, "Failed to leave room");
                    break;
            }
            Context.Abort();
        }

        public async Task SyncVideoState(string roomId, double currentTime, bool isPlaying)
        {
            var (isSuccess, syncMessage) = _roomManager.UpdateVideoState(roomId, currentTime, isPlaying);
            if (isSuccess)
            {
                await Clients.OthersInGroup(roomId).SendAsync(syncMessage, currentTime);
            }
        }

        public async Task SyncTime(string roomId, double currentTime, double playbackRate)
        {
            var roomState = _roomManager.SyncTime(roomId, currentTime);
            if (roomState != null && roomState.HostConnId == Context.ConnectionId)
            {
                await Clients.OthersInGroup(roomId).SendAsync("HostTimeSync", currentTime, playbackRate);
            }
        }

        public async Task SendMessage(string roomId, string userId, string content)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            if (!_roomManager.SaveMessage(roomId, _mapper.Map<UserDto>(user), content, out var message))
            {
                await SendErrorMessage(Context.ConnectionId, "Failed to save message");
                return;
            }

            await Clients.Group(roomId).SendAsync("ReceiveMessage", message);
        }

        public async Task BanUser(string roomId, string userId)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }
            
            if (_roomManager.BanUser(roomId, user.Id, out string connId, out List<User> members))
            {
                await Clients.Client(connId).SendAsync("YouAreBanned");
                await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(members));

            }
        }

        public async Task AddVideoToPlaylist(string roomId, int videoId)
        {
            Video? video = await _videoRepository.GetVideoWithInclude(videoId);

            if (video == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Video not found");
                return;
            }

            if (_roomManager.AddVideoToPlaylist(roomId, _mapper.Map<VideoDto>(video), out var playList))
            {
                await Clients.Group(roomId).SendAsync("PlayListChanged", playList);
            }
            else
            {
                await SendErrorMessage(Context.ConnectionId, "An error happend");
            }
        }

        public async Task StartVideo(string roomId, int videoId)
        {
            Video? video = await _videoRepository.FindByIdAsync(videoId);
            if (video == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Video not found");
                return;
            }
            if (!_roomManager.StartVideo(roomId, video))
            {
                await SendErrorMessage(Context.ConnectionId, "An error happend");
                return;
            }
            await Clients.Group(roomId).SendAsync("StartVideo", video);
        }

        public async Task RemoveVideoFromPlayList(string roomId, int videoId)
        {
            if (_roomManager.RemoveVideoFromPlayList(roomId, videoId, out var playList))
            {
                await Clients.Group(roomId).SendAsync("PlayListChanged", playList);
            }
            else
            {
                await SendErrorMessage(Context.ConnectionId, "An error happend");
            }
        }

        public async Task NextVideo(string roomId)
        {
            VideoDto? video = _roomManager.PlayNextVideo(roomId);
            if (video != null)
            {
                await Clients.Group(roomId).SendAsync("StartVideo", video);
            }
        }

        public async Task PlaybackRateChanged(string roomId, double value)
        {
            if (_roomManager.IsRoomExist(roomId))
            {
                await Clients.Group(roomId).SendAsync("PlaybackRateChanged", value);
            }
        }


        private async Task SendErrorMessage(string connectionId, string message) 
        {
            await Clients.Clients(connectionId).SendAsync("Error", message);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return base.OnDisconnectedAsync(exception);
            }

            if (_roomManager.RemoveUserByUserId(userId, out string? roomId, out List<User>? members))
            {
                if (roomId != null)
                {
                    Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(members));
                }
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
