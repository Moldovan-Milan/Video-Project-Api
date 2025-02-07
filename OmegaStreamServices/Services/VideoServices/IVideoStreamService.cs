using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoStreamService
    {
        Task<(Stream fileStream, string contentType)> GetFileStreamAsync(string folder, string fileName);
        Task<(Stream imageStream, string contentType)> GetThumbnailStreamAsync(int imageId);
        Task<(Stream videoStream, string contentType)> GetVideoStreamAsync(string videoKey);
        Task<(Stream segmentStream, string contentType)> GetVideoSegmentAsync(string segmentKey);
    }
}
