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
            var videos = await _dbSet.Include(v => v.User).Include(v => v.Thumbnail).ToListAsync();
            videos.ForEach(v => v.User.PasswordHash = String.Empty);
            return videos;
        }

        public async Task<Video> GetVideoWithInclude(int id)
        {
            Video video = await _dbSet.Include(v => v.User).Include(v => v.Thumbnail)
                .FirstOrDefaultAsync(v => v.Id == id)!;
            video.User.PasswordHash = String.Empty;
            return video;
        }
    }
}
