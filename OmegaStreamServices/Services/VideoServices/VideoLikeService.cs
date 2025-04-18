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
        private readonly IGenericRepository _repo;

        public VideoLikeService(UserManager<User> userManager, IGenericRepository repo)
        {
            _repo = repo;
        }

        public async Task<string> IsUserLikedVideo(string userId, int videoId)
        {
            var like = await _repo.FindWithKeysAsync<VideoLikes>(userId, videoId);
            if (like == null)
                return "none";
            return like.IsDislike ? "dislike" : "like";
        }

        public async Task<bool> UpdateUserLikedVideo(int videoId, string userId, string likeValue)
        {
            try
            {
                var videoLikes = await _repo.FirstOrDefaultAsync<VideoLikes>(x => x.VideoId == videoId && x.UserId == userId);

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

                    await _repo.AddAsync(videoLikes);
                }
                else
                {
                    if (likeValue == "none")
                    {
                        await _repo.DeleteAsync(videoLikes);
                        return true;
                    }

                    videoLikes.IsDislike = likeValue == "dislike";
                    await _repo.UpdateAsync(videoLikes);
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