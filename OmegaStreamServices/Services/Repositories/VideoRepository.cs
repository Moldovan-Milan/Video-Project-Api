using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;

namespace OmegaStreamServices.Services.Repositories
{
    public class VideoRepository : IVideoRepository
    {
        private readonly AppDbContext _context;
        private readonly Random random = new();

        public VideoRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Video>> GetAllVideosWithIncludes(int pageNumber, int pageSize, bool isShorts)
        {
            var videos = await _context.Videos
                .Where(x => x.IsShort == isShorts)
                .Include(v => v.User).ThenInclude(u => u.Avatar)
                .Include(v => v.Thumbnail)
                .Include(v => v.Comments)
                .Include(v => v.VideoLikes)
                .ToListAsync();

            return videos
                .OrderByDescending(v => CalculateScore(
                    v.Views,
                    v.VideoLikes.Count(l => !l.IsDislike),
                    v.VideoLikes.Count(l => l.IsDislike),
                    v.Comments.Count,
                    (DateTime.UtcNow - v.Created).TotalDays,
                    (DateTime.UtcNow - v.Created).TotalHours))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task<List<Video>> GetVideosByName(string name, int pageNumber, int pageSize, bool isShorts)
        {
            name = name.ToLower();

            var videos = await _context.Videos
                .Where(v => v.Title.ToLower().Contains(name) && v.IsShort == isShorts)
                .Include(v => v.User).ThenInclude(u => u.Avatar)
                .Include(v => v.Thumbnail)
                .Include(v => v.Comments)
                .Include(v => v.VideoLikes)
                .ToListAsync();

            return videos
                .OrderByDescending(v => CalculateScore(
                    v.Views,
                    v.VideoLikes.Count(l => !l.IsDislike),
                    v.VideoLikes.Count(l => l.IsDislike),
                    v.Comments.Count,
                    (DateTime.UtcNow - v.Created).TotalDays,
                    (DateTime.UtcNow - v.Created).TotalHours))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        public async Task DeleteVideoWithRelationsAsync(Video video)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private double CalculateScore(long V, int L, int D, int C, double time, double hours)
        {
            double safeTime = Math.Max(time, 1);
            double score = ((V + 100) * 1.5 + L * 2 - D * 2 + C * 0.5)
                           / (2 + Math.Pow(safeTime, 0.2));

            if (hours < 1)
                score *= 2;
            if (random.Next(0, 99) == 10)
                score *= 10;

            return score;
        }
    }
}
