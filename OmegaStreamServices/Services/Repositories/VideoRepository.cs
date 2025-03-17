using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class VideoRepository : BaseRepository<Video>, IVideoRepository
    {
        private readonly Random random;

        public VideoRepository(AppDbContext context) : base(context)
        {
            random = new Random();
        }

        public async Task<List<Video>> GetAllVideosWithIncludes(int pageNumber, int pageSize)
        {
            return await Task.FromResult(_dbSet
                .Include(v => v.User)
                .Include(v => v.Thumbnail)
                .Include(v => v.Comments)
                .Include(v => v.VideoLikes)
                .AsEnumerable() // Exit from sql query
                .OrderByDescending(v => CalculateScore(
                    v.Views,
                    v.VideoLikes.Count(l => !l.IsDislike),
                    v.VideoLikes.Count(l => l.IsDislike),
                    v.Comments.Count,
                    (DateTime.UtcNow - v.Created).TotalDays,
                    (DateTime.UtcNow - v.Created).TotalHours))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList());
        }

        public async Task<List<Video>> GetVideosByName(string name, int pageNumber, int pageSize)
        {
            name = name.ToLower();
            return await Task.FromResult(_dbSet
                .Where(v => v.Title.ToLower().Contains(name))
                .Include(v => v.User)
                .Include(v => v.Thumbnail)
                .Include(v => v.Comments)
                .Include(v => v.VideoLikes)
                .AsEnumerable() // Exit from sql query
                .OrderByDescending(v => CalculateScore(
                    v.Views,
                    v.VideoLikes.Count(l => !l.IsDislike),
                    v.VideoLikes.Count(l => l.IsDislike),
                    v.Comments.Count,
                    (DateTime.UtcNow - v.Created).TotalDays,
                    (DateTime.UtcNow - v.Created).TotalHours))
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList());
        }

        public async Task<Video?> GetVideoWithInclude(int id)
        {
            return await _dbSet
                .Include(v => v.User)
                .Include(v => v.Thumbnail)
                .Include(v => v.Comments)
                    .ThenInclude(c => c.User)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task DeleteVideoWithRelationsAsync(Video video)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _dbSet.Remove(video);
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
            if (random.Next(0, 99) == 0) // 1% chance to increase the points by 10x
                score *= 10;

            return score;
        }
    }
}
