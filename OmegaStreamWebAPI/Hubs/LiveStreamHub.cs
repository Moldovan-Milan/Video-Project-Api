using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Hubs
{
    public class LiveStreamHub : BaseHub
    {
        private readonly UserManager<User> _userManager;
        private readonly ILiveStreamRepository _liveStreamRepository;
        private readonly IMapper _mapper;

        public LiveStreamHub(UserManager<User> userManager, ILiveStreamRepository liveStreamRepository, IMapper mapper)
        {
            _userManager = userManager;
            _liveStreamRepository = liveStreamRepository;
            _mapper = mapper;
        }

        [Authorize]
        public async Task StartStream(string userId, string title, string? desc)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(title))
            {
                await SendErrorMessage(Context.ConnectionId, "Title or userId is empty!");
                return;
            }

            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            var liveStreamId = Guid.NewGuid().ToString();
            var liveStream = new LiveStream
            {
                Id = liveStreamId,
                StreamerConnectionId = Context.ConnectionId,
                UserId = user.Id,
                User = user,
                StartedAt = DateTime.UtcNow,
                StreamTitle = title,
                Description = desc ?? string.Empty
            };

            await Groups.AddToGroupAsync(Context.ConnectionId, liveStreamId);
            await _liveStreamRepository.AddLiveStreamAsync(liveStream);
            await Clients.Caller.SendAsync("LiveStreamStarted", liveStreamId);
            Console.WriteLine($"Stream started: {liveStreamId}, Streamer: {Context.ConnectionId}");
        }

        [Authorize]
        public async Task StopStream(string userId)
        {
            LiveStream? liveStream = await _liveStreamRepository.GetLiveStreamByUserIdAsync(userId);
            if (liveStream == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Stream not found");
                return;
            }

            await _liveStreamRepository.RemoveLiveStreamAsync(liveStream.Id);
            await Clients.Group(liveStream.Id).SendAsync("StreamStopped");
            Console.WriteLine($"Stream stopped: {liveStream.Id}");
        }

        [AllowAnonymous]
        public async Task WatchStream(string streamId)
        {
            LiveStream? liveStream = await _liveStreamRepository.GetLiveStreamByIdAsync(streamId);
            if (liveStream == null)
            {
                await Clients.Caller.SendAsync("LiveNotFound");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, liveStream.Id);
            await Clients.Caller.SendAsync("ReceiveChatHistory", liveStream.Messages);
            await Clients.Clients(liveStream.StreamerConnectionId).SendAsync("ReceiveViewer", Context.ConnectionId);
            liveStream.Viewers++;
            liveStream.ViewersConnectionIds.Add(Context.ConnectionId);
            await Clients.Groups(liveStream.Id).SendAsync("ViewerCountChanged", liveStream.Viewers);
        }

        [AllowAnonymous]
        public async Task LeaveStream(string streamId)
        {
            LiveStream? liveStream = await _liveStreamRepository.GetLiveStreamByIdAsync(streamId);
            if (liveStream == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Stream not found");
                return;
            }
            await Clients.Client(liveStream.StreamerConnectionId).SendAsync("ViewerLeftStream", Context.ConnectionId);
            liveStream.Viewers--;
            liveStream.ViewersConnectionIds.Remove(Context.ConnectionId);
            await Clients.Groups(liveStream.Id).SendAsync("ViewerCountChanged", liveStream.Viewers);
            //Context.Abort();
        }

        [Authorize]
        public async Task SendMessage(string userId, string message, string streamId)
        {
            User? user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                await SendErrorMessage(Context.ConnectionId, "User not found");
                return;
            }

            LiveStream? liveStream = await _liveStreamRepository.GetLiveStreamByIdAsync(streamId);
            if (liveStream == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Stream not found");
                return;
            }

            RoomMessage roomMessage = new RoomMessage
            {
                Content = message,
                Sender = _mapper.Map<UserDto>(user),
            };

            liveStream.Messages.Add(roomMessage);
            await Clients.Group(liveStream.Id).SendAsync("ReceiveMessage", roomMessage);
        }

        #region WebRTC

        [AllowAnonymous]
        public async Task SendOffer(string connectionId, string offer)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveOffer", offer, Context.ConnectionId);
        }

        [AllowAnonymous]
        public async Task SendAnswer(string connectionId, string answer)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveAnswer", answer, Context.ConnectionId);
        }

        [AllowAnonymous]
        public async Task SendIceCandidate(string connectionId, string candidate)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveIceCandidate", candidate, Context.ConnectionId);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            string? userIdFromToken = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken != null)
            {
                // User is authenticated, handle disconnection
                LiveStream? liveStream = _liveStreamRepository.GetLiveStreamByUserIdAsync(userIdFromToken).Result;
                if (liveStream != null)
                {
                    // User is a streamer, handle streamer disconnection
                    liveStream.EndedAt = DateTime.UtcNow;
                    _liveStreamRepository.UpdateLiveStreamAsync(liveStream).Wait();
                    Clients.Group(liveStream.Id).SendAsync("StreamStopped").Wait();
                    _liveStreamRepository.RemoveLiveStreamAsync(liveStream.Id).Wait();
                    Console.WriteLine($"Streamer disconnected: {liveStream.Id}");
                }
                else
                {
                    // User is a viewer, handle viewer disconnection
                    liveStream = _liveStreamRepository.GetLiveStreamByConnectionIdAsync(Context.ConnectionId).Result;
                    if (liveStream != null)
                    {
                        liveStream.Viewers--;
                        liveStream.ViewersConnectionIds.Remove(Context.ConnectionId);
                        _liveStreamRepository.UpdateLiveStreamAsync(liveStream).Wait();
                        Clients.Group(liveStream.Id).SendAsync("ViewerCountChanged", liveStream.Viewers).Wait();
                        Clients.Client(liveStream.StreamerConnectionId).SendAsync("ViewerLeftStream", Context.ConnectionId).Wait();
                        Console.WriteLine($"Viewer disconnected: {liveStream.Id}");
                    }
                }
            }

            return base.OnDisconnectedAsync(exception);
        }

        #endregion WebRTC
    }
}
