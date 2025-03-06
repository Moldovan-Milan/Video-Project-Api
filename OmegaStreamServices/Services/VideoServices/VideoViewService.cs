﻿using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoViewService : IVideoViewService
    {

        private readonly IVideoViewRepository _videoViewRepository;
        private readonly IVideoRepository _videoRepository;
        private readonly UserManager<User> _userManager;

        public VideoViewService(IVideoViewRepository videoViewRepository, IVideoRepository videoRepository, UserManager<User> userManager)
        {
            _videoViewRepository = videoViewRepository;
            _videoRepository = videoRepository;
            _userManager = userManager;
        }
        public async Task<bool> ValidateView(VideoView view)
        {
            view.Video = await _videoRepository.GetVideoWithInclude(view.VideoId);
            if (view.Video == null)
            {
                Console.WriteLine("Video not found.");
                return false;
            }

            if (view.UserId == null)
            {
                _videoViewRepository.RemoveOutdatedGuestViews();
                var lastGuestView = VideoViewRepository.GuestViews
                    .Where(v => v.IpAddressHash == view.IpAddressHash)
                    .OrderByDescending(v => v.ViewedAt)
                    .FirstOrDefault();

                if (lastGuestView == null ||
                    ToUnixMillis(DateTime.UtcNow) - ToUnixMillis(lastGuestView.ViewedAt) > VideoViewRepository.ViewCooldown * 1000)
                {
                    _videoViewRepository.AddGuestView(view);
                    view.Video.Views++;
                    _videoRepository.Update(view.Video);
                    return true;
                }
                else
                {
                    Console.WriteLine("Guest view cooldown not met.");
                }
            }
            else
            {
                view.User = await _userManager.FindByIdAsync(view.UserId);
                if (view.User == null)
                {
                    Console.WriteLine("User not found.");
                    return false;
                }

                var lastUserView = await _videoViewRepository.GetLastUserVideoView(view.UserId, view.VideoId);
                if (lastUserView == null ||
                    ToUnixMillis(DateTime.UtcNow) - ToUnixMillis(lastUserView.ViewedAt) > VideoViewRepository.ViewCooldown * 1000)
                {
                    await _videoViewRepository.Add(view);
                    view.Video.Views++;
                    _videoRepository.Update(view.Video);
                    return true;
                }
                else
                {
                    Console.WriteLine("User view cooldown not met.");
                }
            }
            return false;
        }


        private long ToUnixMillis(DateTime dateTime)
        {
            DateTime utcDateTime = dateTime.ToUniversalTime();

            DateTimeOffset epoch = new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero);

            long unixMilliseconds = (long)(utcDateTime - epoch).TotalMilliseconds;

            return unixMilliseconds;
        }
    }
}
