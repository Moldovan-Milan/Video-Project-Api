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
    public class ImageRepository : BaseRepository<Image>, IImageRepository
    {
        public ImageRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Image> FindImageByPath(string path)
        {
           return await _dbSet.FirstOrDefaultAsync(i => i.Path == path)!;
        }
    }
}
