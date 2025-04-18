using AutoMapper;
using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Dto;
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
        private readonly IGenericRepository _repo;
        private readonly IVideoViewRepository _videoViewRepository;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;

        public VideoViewService(IVideoViewRepository videoViewRepository, UserManager<User> userManager, IMapper mapper, IGenericRepository repo)
        {
            _videoViewRepository = videoViewRepository;
            _userManager = userManager;
            _mapper = mapper;
            _repo = repo;
        }

        public async Task<List<VideoViewDto>> GetUserViewHistory(string userId, int? pageNumber, int? pageSize)
        {
            pageNumber = pageNumber ?? 1;
            pageSize = pageSize ?? 30;
            if (pageNumber <= 0) {
                pageNumber = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 30;
            }
            var videoViews = await _videoViewRepository.GetUserViewHistory(userId,pageNumber.Value,pageSize.Value);
            return _mapper.Map<List<VideoViewDto>>(videoViews);
        }


        public async Task<bool> ValidateView(VideoView view)
        {
            Video? video = await _repo.FirstOrDefaultAsync<Video>(predicate: x => x.Id == view.VideoId);

            if (video == null)
            {
                return false;
            }
            view.Video = video;

            if (view.UserId == null)
            {
                _videoViewRepository.RemoveOutdatedGuestViews();
                var lastGuestView = _videoViewRepository.GuestViews
                    .Where(v => v.IpAddressHash == view.IpAddressHash)
                    .OrderByDescending(v => v.ViewedAt)
                    .FirstOrDefault();

                if (lastGuestView == null ||
                    ToUnixMillis(DateTime.UtcNow) - ToUnixMillis(lastGuestView.ViewedAt) > VideoViewRepository.ViewCooldown * 1000)
                {
                    _videoViewRepository.AddGuestView(view);
                    view.Video.Views++;
                    await _repo.UpdateAsync(view.Video);
                    return true;
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
                    await _videoViewRepository.AddLoggedInVideoView(view);
                    view.Video.Views++;
                    //_videoRepository.Update(view.Video);
                    await _repo.UpdateAsync(view.Video);
                    return true;
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
