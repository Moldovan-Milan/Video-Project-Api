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
        public async Task<List<VideoView>> GetUserViewHistory(string userId)
        {
            var videos = await _dbSet.Where(x => x.UserId == userId).ToListAsync();
            return videos;
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
    }
}
