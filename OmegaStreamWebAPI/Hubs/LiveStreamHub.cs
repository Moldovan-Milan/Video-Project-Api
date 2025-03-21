﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;

namespace OmegaStreamWebAPI.Hubs
{
    public class LiveStreamHub : BaseHub
    {
        private readonly UserManager<User> _userManager;
        private readonly ILiveStreamRepository _liveStreamRepository;

        public LiveStreamHub(UserManager<User> userManager, ILiveStreamRepository liveStreamRepository)
        {
            _userManager = userManager;
            _liveStreamRepository = liveStreamRepository;
        }

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

            await _liveStreamRepository.AddLiveStreamAsync(liveStream);
            await Clients.Caller.SendAsync("LiveStreamStarted", liveStreamId);
            Console.WriteLine($"Stream started: {liveStreamId}, Streamer: {Context.ConnectionId}");
        }

        public async Task StopStream(string userId)
        {
            LiveStream? liveStream = await _liveStreamRepository.GetLiveStreamByUserIdAsync(userId);
            if (liveStream == null)
            {
                await SendErrorMessage(Context.ConnectionId, "Stream not found");
                return;
            }

            await _liveStreamRepository.RemoveLiveStreamAsync(liveStream.Id);
            await Clients.Caller.SendAsync("StreamStopped");
            Console.WriteLine($"Stream stopped: {liveStream.Id}");
        }

        public async Task WatchStream(string streamId)
        {
            LiveStream? liveStream = await _liveStreamRepository.GetLiveStreamByIdAsync(streamId);
            if (liveStream == null)
            {
                await Clients.Caller.SendAsync("LiveNotFound");
                return;
            }

            await Clients.Clients(liveStream.StreamerConnectionId).SendAsync("ReceiveViewer", Context.ConnectionId);
        }

        public async Task SendOffer(string connectionId, string offer)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveOffer", offer, Context.ConnectionId);
        }

        public async Task SendAnswer(string connectionId, string answer)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveAnswer", answer, Context.ConnectionId);
        }

        public async Task SendIceCandidate(string connectionId, string candidate)
        {
            await Clients.Client(connectionId).SendAsync("ReceiveIceCandidate", candidate, Context.ConnectionId);
        }
    }
}
