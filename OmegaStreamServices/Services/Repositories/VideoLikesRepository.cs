using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class VideoLikesRepository : BaseRepository<VideoLikes>, IVideoLikesRepository
    {
        public VideoLikesRepository(AppDbContext context) : base(context)
        {

        }

        public async Task<int> GetDisLikesByVideoId(int videoId)
        {
            return await GetLikeNumbers(true, videoId);
        }

        public async Task<int> GetLikesByVideoId(int videoId)
        {
            return await GetLikeNumbers(false, videoId);
        }

        public async Task<VideoLikes> GetVideoLike(string userId, int videoId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.VideoId == videoId && x.UserId == userId);

        }

        public async Task<List<VideoLikes>> GetAllReactionsByVideo(int videoId)
        {
            return await _dbSet.Where(r => r.VideoId == videoId).ToListAsync();
        }

        public async Task<string> IsLikedByUser(string userId, int videoId)
        {
            string result = "none";

            VideoLikes videoLikes = await _dbSet.FindAsync(userId, videoId);
            if (videoLikes == null)
                return result;
            if (videoLikes.IsDislike)
                result = "dislike";
            else
                result = "like";

            return result;
        }

        private async Task<int> GetLikeNumbers(bool isDisklike, int videoId)
        {
            return await _dbSet.Where(vl => vl.VideoId == videoId 
                 && vl.IsDislike == isDisklike)
                .CountAsync();
        }
    }
}
