﻿using OmegaStreamServices.Models;
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

        public VideoViewService(IVideoViewRepository videoViewRepository)
        {
            _videoViewRepository = videoViewRepository;
        }
        public async Task ValidateView(VideoView view)
        {
            if (view.UserId == null) {
                _videoViewRepository.RemoveOutdatedGuestViews();
                if (!VideoViewRepository.GuestViews.Any(v => v.IpAddressHash == view.IpAddressHash))
                {
                    _videoViewRepository.AddGuestView(view);
                    view.Video.Views++;
                }
            }
            else
            {
                if(ToUnixMillis(DateTime.UtcNow) - ToUnixMillis(view.ViewedAt) > VideoViewRepository.ViewCooldown*1000)
                {
                    await _videoViewRepository.Add(view);
                    view.Video.Views++;
                }
                
            }
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
