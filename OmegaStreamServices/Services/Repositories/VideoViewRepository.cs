using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class VideoViewRepository : BaseRepository<VideoView>, IVideoViewRepository
    {
        public VideoViewRepository(AppDbContext context) : base(context)
        {
            
        }
        public async Task<List<VideoView>> GetUserViewHistory(string userId)
        {
            var videos = await _dbSet.Where(x => x.UserId == userId).ToListAsync();
            return videos;
        }
    }
}
