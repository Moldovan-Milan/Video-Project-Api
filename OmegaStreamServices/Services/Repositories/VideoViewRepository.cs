using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmegaStreamServices.Services.VideoServices;
using Microsoft.Extensions.Caching.Memory;

namespace OmegaStreamServices.Services.Repositories
{
    public class VideoViewRepository : BaseRepository<VideoView>, IVideoViewRepository
    {
        private readonly IMemoryCache _cache;
        private static readonly string GuestViewsCacheKey = "GuestViews";
        public static int ViewCooldown { get; } = 30;
        public List<VideoView> GuestViews { get; set; }

        
        public VideoViewRepository(AppDbContext context, IMemoryCache cache) : base(context)
        {
            _cache = cache;
            GuestViews =  _cache.GetOrCreate(GuestViewsCacheKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromSeconds(ViewCooldown);
                return new List<VideoView>();
            });
        }
        public async Task<List<VideoView>> GetUserViewHistory(string userId, int pageNumber, int pageSize)
        {
            var videoViews = await _dbSet
                .Where(x => x.UserId == userId)
                .Include(x => x.User)
                .Include(x => x.Video)
                .ThenInclude(v => v.User)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderByDescending(x => x.ViewedAt)
                .ToListAsync();

            return videoViews;
        }

        public void RemoveOutdatedGuestViews()
        {
            var now = DateTime.UtcNow;
            GuestViews.RemoveAll(view => (now - view.ViewedAt).TotalSeconds > ViewCooldown);
        }
        public void AddGuestView(VideoView view)
        {
            GuestViews.Add(view);
            _cache.Set(GuestViewsCacheKey, GuestViews);
        }

        public async Task<VideoView> GetLastUserVideoView(string userId, int videoId)
        {
            var view = await _dbSet
                .Where(x => x.UserId == userId && x.VideoId == videoId)
                .OrderByDescending(x => x.ViewedAt)
                .FirstOrDefaultAsync();

            return view;
        }

        public async Task AddLoggedInVideoView(VideoView view)
        {
            var existingViews = await _dbSet
                .Where(v => v.UserId == view.UserId && v.VideoId == view.VideoId)
                .ToListAsync();

            if (existingViews.Any())
            {
                _dbSet.RemoveRange(existingViews);
            }

            _dbSet.Add(view);
        }
    }
}
