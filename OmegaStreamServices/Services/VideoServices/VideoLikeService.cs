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

        public VideoLikeService(IVideoLikesRepository videoLikesRepository, UserManager<User> userManager)
        {
            _videoLikesRepository = videoLikesRepository;
        }

        public async Task<string> IsUserLikedVideo(string userId, int videoId)
        {
            return await _videoLikesRepository.IsLikedByUser(userId, videoId);
        }

        public async Task<bool> UpdateUserLikedVideo(int videoId, string userId, string likeValue)
        {
            try
            {
                var videoLikes = await _videoLikesRepository.GetVideoLike(userId, videoId);

                if (videoLikes == null)
                {
                    // Nem létezik, ezért semmi nem kell tennünk
                    // Ez elméletileg sosem fog igaz lenni, de
                    // inkább kezeljük le
                    if (likeValue == "none")
                    {
                        return false;
                    }

                    videoLikes = new VideoLikes
                    {
                        UserId = userId,
                        VideoId = videoId,
                        IsDislike = likeValue == "dislike"
                    };

                    await _videoLikesRepository.Add(videoLikes);
                }
                else
                {
                    if (likeValue == "none")
                    {
                        _videoLikesRepository.Delete(videoLikes);
                        return true;
                    }

                    videoLikes.IsDislike = likeValue == "dislike";
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