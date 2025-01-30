﻿using Microsoft.EntityFrameworkCore;
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

        public async Task<List<Video>> GetAllVideosWithIncludes()
        {
            var videos = await _dbSet.Include(v => v.User).Include(v => v.Thumbnail)
.ToListAsync();
            return videos;
        }

        public Task<List<Video>> GetVideosByName(string name)
        {
            name = name.ToLower();
            return _dbSet.Where(x => x.Title.ToLower().Contains(name))
                .Include(x => x.User).Include(x => x.Thumbnail).ToListAsync();
        }

        public async Task<Video> GetVideoWithInclude(int id)
        {
            Video video = await _dbSet.Include(v => v.User).Include(v => v.Thumbnail)
                .Include(x => x.Comments).ThenInclude(x => x.User)
                .FirstOrDefaultAsync(v => v.Id == id)!;
            video.User.PasswordHash = String.Empty;
            return video;
        }
    }
}
