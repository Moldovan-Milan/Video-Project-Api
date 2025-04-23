using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IVideoRepository
    {
        Task<List<Video>> GetAllVideosWithIncludes(int pageNumber, int pageSize, bool isShorts);
        Task<List<Video>> GetVideosByName(string name, int pageNumber, int pageSize, bool isShorts);
        Task DeleteVideoWithRelationsAsync(Video video);
    }
}
