using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoLikeService : IVideoLikeService
    {
        private readonly IVideoLikesRepository _videoLikesRepository;
        private readonly UserManager<User> _userManager;

        public VideoLikeService(IVideoLikesRepository videoLikesRepository, UserManager<User> userManager)
        {
            _videoLikesRepository = videoLikesRepository;
            _userManager = userManager;
        }

        public async Task<string> IsUserLikedVideo(string userId, int videoId)
        {
            return await _videoLikesRepository.IsLikedByUser(userId, videoId);
        }

        public async Task<bool> UpdateUserLikedVideo(int videoId, string userId, string likeValue)
        {
            try
            {
                bool isLikeExist = true;
                VideoLikes videoLikes = await _videoLikesRepository.GetVideoLike(userId, videoId);
                if (videoLikes != null && likeValue == "none")
                {
                    _videoLikesRepository.Delete(videoLikes);
                    return true;
                }
                if (videoLikes == null && likeValue == "none")
                {
                    return false;
                }
                if (videoLikes == null && likeValue != "none")
                {
                    isLikeExist = false;
                    videoLikes = new VideoLikes
                    {
                        UserId = userId,
                        VideoId = videoId,
                    };
                }

                switch (likeValue)
                {
                    case "like":
                        videoLikes.IsDislike = false;
                        break;
                    case "dislike":
                        videoLikes.IsDislike = true;
                        break;
                }
                // Ha nincs felvéve, akkor létre kell hozni, hogy ne fusson le hibával
                if (!isLikeExist)
                {
                    await _videoLikesRepository.Add(videoLikes);
                }
                else
                {
                    _videoLikesRepository.Update(videoLikes);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}