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
    public class CommentRepository : BaseRepository<Comment>, ICommentRepositroy
    {
        public CommentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<Comment>> GetAllCommentsByVideo(int videoId)
        {
            var comments = await _dbSet
                .Where(c => c.VideoId == videoId)
                .ToListAsync();

            return comments;
        }
    }
}
