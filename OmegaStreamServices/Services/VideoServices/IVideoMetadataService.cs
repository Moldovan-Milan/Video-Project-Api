using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoMetadataService
    {
        Task<List<VideoDto?>> GetAllVideosMetaData(int? pageNumber, int? pageSize);
        Task<VideoDto?> GetVideoMetaData(int id);
        Task<List<VideoDto?>> GetVideosByName(string name, int? pageNumber, int? pageSize);
    }
}
