using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.UserServices;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Hubs
{
    public class WatchTogetherHub : BaseHub
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

        private async Task SendErrorMessage(string connectionId, string message)
            => await Clients.Client(connectionId).SendAsync("Error", message);

        /// <summary>
        /// Handles the room join logic based on the result of adding a user to a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room the user is trying to join.
        /// </param>
        /// <param name="user">
        /// The user who is trying to join the room.
        /// </param>
        /// <param name="roomState">
        /// The current state of the room.
        /// </param>
        /// <param name="result">
        /// The result of the attempt to add the user to the room.
        /// </param>
        /// <returns></returns>
        private async Task HandleRoomJoin(string roomId, User user, RoomState roomState, RoomStateResult result)
        {
            switch (result)
            {
                case RoomStateResult.Created:
                    await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                    await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                    await Clients.Caller.SendAsync("YouAreHost");
                    break;

                case RoomStateResult.HostReconected:
                    await HandleHostReconnection(roomId, roomState);
                    break;

                case RoomStateResult.Banned:
                    await Clients.Caller.SendAsync("YouAreBanned");
                    break;

                case RoomStateResult.NeedsAproval:
                    await Clients.Client(roomState.HostConnId).SendAsync("JoinRequest", _mapper.Map<UserDto>(user));
                    break;

                case RoomStateResult.RoomIsFull:
                    await SendErrorMessage(Context.ConnectionId, "Room is full");
                    break;

                case RoomStateResult.Failed:
                    await Clients.Caller.SendAsync("ConnectionFailed");
                    break;
            }
        }

        /// <summary>
        /// Handles the reconnection of a host to a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room the host is reconnecting to.
        /// </param>
        /// <param name="roomState">
        /// The current state of the room.
        /// </param>
        /// <returns></returns>
        private async Task HandleHostReconnection(string roomId, RoomState roomState)
        {
            if (roomState != null)
            {
                await Clients.Caller.SendAsync("YouAreHost", roomState.RoomMessages);
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                await Clients.OthersInGroup(roomId).SendAsync("HostInRoom");
            }
        }

        /// <summary>
        /// Handles the logic for a user joining a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room the user is trying to join.
        /// </param>
        /// <param name="userId">
        /// The ID of the user who is trying to join the room.
        /// </param>
        /// <returns></returns>
        public async Task JoinRoom(string roomId, string userId)
        {
            var user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            var result = _roomManager.AddUserToRoom(roomId, user, Context.ConnectionId, out var roomState);
            await HandleRoomJoin(roomId, user, roomState, result);
        }

        /// <summary>
        /// Handles the logic for accepting a user into a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room the user is trying to join.
        /// </param>
        /// <param name="userId">
        /// The ID of the user who is trying to join the room.
        /// </param>
        /// <returns></returns>
        public async Task AcceptUser(string roomId, string userId)
        {
            User? user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
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
                await Clients.Client(connId).SendAsync("RequestAccepted", roomState.VideoState.CurrentTime, roomState.VideoState.IsPlaying, roomState.RoomMessages, roomState.PlayList, currentVideo);
                await Clients.Group(roomId).SendAsync("JoinedToRoom", _mapper.Map<List<UserDto>>(roomState.Members));
            }
            else
            {
                await SendErrorMessage(Context.ConnectionId, result == RoomStateResult.RoomIsFull ? "Room is full" : "Failed to accept");
            }
        }

        /// <summary>
        /// Handles the logic for rejecting a user from a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room the user is trying to join.
        /// </param>
        /// <param name="userId">
        /// The ID of the user who is trying to join the room.
        /// </param>
        /// <returns></returns>
        public async Task RejectUser(string roomId, string userId)
        {
            var result = _roomManager.RejectUser(roomId, userId, out string connId, out _);
            if (result)
            {
                await Clients.Client(connId).SendAsync("RequestRejected");
            }
        }

        /// <summary>
        /// Handles the logic for a user leaving a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room the user is trying to leave.
        /// </param>
        /// <param name="userId">
        /// The ID of the user who is trying to leave the room.
        /// </param>
        /// <returns></returns>
        public async Task LeaveRoom(string roomId, string userId)
        {
            var result = _roomManager.RemoveUserFromRoom(roomId, userId, Context.ConnectionId, out var roomState);
            switch (result)
            {
                case RoomStateResult.Accepted:
                    await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(roomState.Members));
                    break;
                case RoomStateResult.HostLeft:
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

        /// <summary>
        /// Synchronizes the video state across all clients in the room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="currentTime">
        /// The current time of the video.
        /// </param>
        /// <param name="isPlaying">
        /// Whether the video is currently playing or paused.
        /// </param>
        /// <returns></returns>
        public async Task SyncVideoState(string roomId, double currentTime, bool isPlaying)
        {
            var (isSuccess, syncMessage) = _roomManager.UpdateVideoState(roomId, currentTime, isPlaying);
            if (isSuccess)
            {
                await Clients.OthersInGroup(roomId).SendAsync(syncMessage, currentTime);
            }
        }


        /// <summary>
        /// Synchronizes the time across all clients in the room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="currentTime">
        /// The current time of the video.
        /// </param>
        /// <param name="playbackRate">
        /// The playback rate of the video.
        /// </param>
        /// <returns></returns>
        public async Task SyncTime(string roomId, double currentTime, double playbackRate)
        {
            var roomState = _roomManager.SyncTime(roomId, currentTime);
            if (roomState?.HostConnId == Context.ConnectionId)
            {
                await Clients.OthersInGroup(roomId).SendAsync("HostTimeSync", currentTime, playbackRate);
            }
        }


        /// <summary>
        /// Sends a message to all clients in the room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the user tries to send message.
        /// </param>
        /// <param name="userId">
        /// The ID of the user who is sending the message.
        /// </param>
        /// <param name="content">
        /// The content of the message.
        /// </param>
        /// <returns></returns>
        public async Task SendMessage(string roomId, string userId, string content)
        {
            User? user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            if (_roomManager.SaveMessage(roomId, _mapper.Map<UserDto>(user), content, out var message))
            {
                await Clients.Group(roomId).SendAsync("ReceiveMessage", message);
            }
            else
            {
                await SendErrorMessage(Context.ConnectionId, "Failed to save message");
            }
        }


        /// <summary>
        /// Handles the logic for banning a user from a room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="userId">
        /// The ID of the user to be banned.
        /// </param>
        /// <returns></returns>
        public async Task BanUser(string roomId, string userId)
        {
            User? user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            if (_roomManager.BanUser(roomId, user.Id, out string connId, out var members))
            {
                await Clients.Client(connId).SendAsync("YouAreBanned");
                await Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(members));
            }
        }


        /// <summary>
        /// Handles the logic for adding a video to the playlist.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="videoId">
        /// The ID of the video to be added to the playlist.
        /// </param>
        /// <returns></returns>
        public async Task AddVideoToPlaylist(string roomId, int videoId)
        {
            var video = await _videoRepository.GetVideoWithInclude(videoId);
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
                await SendErrorMessage(Context.ConnectionId, "An error happened");
            }
        }


        /// <summary>
        /// Handles the logic for starting a video in the room.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="videoId">
        /// The ID of the video to be started.
        /// </param>
        /// <returns></returns>
        public async Task StartVideo(string roomId, int videoId)
        {
            var video = await _videoRepository.FindByIdAsync(videoId);
            if (video == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Video not found");
                return;
            }

            if (!_roomManager.StartVideo(roomId, video))
            {
                await SendErrorMessage(Context.ConnectionId, "An error happened");
                return;
            }

            await Clients.Group(roomId).SendAsync("StartVideo", video);
        }


        /// <summary>
        /// Handles the logic for removing a video from the playlist.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="videoId">
        /// The ID of the video to be removed from the playlist.
        /// </param>
        /// <returns></returns>
        public async Task RemoveVideoFromPlayList(string roomId, int videoId)
        {
            if (_roomManager.RemoveVideoFromPlayList(roomId, videoId, out var playList))
            {
                await Clients.Group(roomId).SendAsync("PlayListChanged", playList);
            }
            else
            {
                await SendErrorMessage(Context.ConnectionId, "An error happened");
            }
        }

        /// <summary>
        /// Handles the logic for playing the next video in the playlist.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <returns></returns>
        public async Task NextVideo(string roomId)
        {
            var video = _roomManager.PlayNextVideo(roomId);
            if (video != null)
            {
                await Clients.Group(roomId).SendAsync("StartVideo", video);
            }
        }

        /// <summary>
        /// Handles the logic for changing the playback rate of the video.
        /// </summary>
        /// <param name="roomId">
        /// The ID of the room that the host is in.
        /// </param>
        /// <param name="value">
        /// The new playback rate value.
        /// </param>
        /// <returns></returns>
        public async Task PlaybackRateChanged(string roomId, double value)
        {
            if (_roomManager.IsRoomExist(roomId))
            {
                await Clients.Group(roomId).SendAsync("PlaybackRateChanged", value);
            }
        }


        /// <summary>
        /// Handles the logic for when a user disconnects from the hub.
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string? userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return base.OnDisconnectedAsync(exception);

            if (_roomManager.RemoveUserByUserId(userId, out string? roomId, out var members) && roomId != null)
            {
                Clients.Group(roomId).SendAsync("LeavedRoom", _mapper.Map<List<UserDto>>(members));
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}
