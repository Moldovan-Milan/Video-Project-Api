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
    public class VideoRepository : BaseRepository<Video>, IVideoRepository
    {
        public VideoRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Video>> GetAllVideosWithIncludes(int pageNumber, int pageSize)
        {
            var videos = await _dbSet
                .Include(v => v.User)
                .Include(v => v.Thumbnail)
                .Include(x => x.Comments)
                .Include(x => x.VideoLikes).ToListAsync();
            videos = await SortVideos(videos);

            return videos.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToList();
        }

        public async Task<List<Video>> GetVideosByName(string name, int pageNumber, int pageSize)
        {
            name = name.ToLower();
            var videos = await _dbSet
                .Where(x => x.Title.ToLower().Contains(name))
                .Include(x => x.User)
                .Include(x => x.Thumbnail)
                .Include(x => x.Comments)
                .Include(x => x.VideoLikes).ToListAsync();

            videos = await SortVideos(videos);

            return videos.Skip((pageNumber - 1) * pageSize)
                .Take(pageSize).ToList();
        }

        public async Task<Video> GetVideoWithInclude(int id)
        {
            Video video = await _dbSet.Include(v => v.User).Include(v => v.Thumbnail)
                .Include(x => x.Comments).ThenInclude(x => x.User)
                .FirstOrDefaultAsync(v => v.Id == id)!;
            return video;
        }

        public Task<List<Video>> SortVideos(List<Video> videos)
        {
            var sortedVideos = videos
                .Select(v => new
                {
                    Video = v,
                    Likes = v.VideoLikes.Count(l => !l.IsDislike),
                    Dislikes = v.VideoLikes.Count(l => l.IsDislike),
                    Views = v.Views,
                    Comments = v.Comments.Count,
                    Days = Math.Max((DateTime.UtcNow.Date - v.Created).TotalDays, 0)
                })
                .OrderByDescending(v => CalculateScore(v.Views, v.Likes, v.Dislikes, v.Comments, v.Days))
                .Select(v => v.Video)
                .ToList();

            return Task.FromResult(sortedVideos);
        }


        /// <summary>
        /// Calculate video score based on views, likes, dislikes, comments, and time.
        /// </summary>
        /// <param name="V">Number of views</param>
        /// <param name="L">Number of likes</param>
        /// <param name="D">Number of dislikes</param>
        /// <param name="C">Number of comments</param>
        /// <param name="time">Age of the video in days</param>
        /// <returns>Calculated score</returns>

        private double CalculateScore(long V, int L, int D, int C, double time)
        {
            double safeTime = Math.Max(time, 1);
            return ((V + 100) * 1.5 + L * 2 - D * 2 + C * 0.5)
                  / (2 + Math.Pow(safeTime, 0.2));
            //return (L - D / V + 1) + C / 10 + Math.Exp(-safeTime / 30.0);
        }

    }
}
