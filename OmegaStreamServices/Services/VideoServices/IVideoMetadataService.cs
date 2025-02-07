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
        Task<List<Video>> GetAllVideosMetaData();
        Task<VideoDto> GetVideoMetaData(int id);
        Task<List<Video>> GetVideosByName(string name);
    }
}
