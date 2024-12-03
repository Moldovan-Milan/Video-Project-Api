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
        Task<(Stream videoStream, string contentType)> GetVideoStreamAsync(string videoKey);
        Task<(Stream segmentStream, string contentType)> GetVideoSegmentAsync(string segmentKey);
        Task<(Stream imageStream, string contentType)> GetImageStreamAsync(int imageId, string path = "thumbnails");

        /// <summary>
        /// Gets all video data from the database.
        /// </summary>
        /// <returns>A list of all videos.</returns>
        Task<List<Video>> GetAllVideosMetaData();

        // <summary>
        /// Gets the video data by its ID.
        /// </summary>
        /// <param name="id">The ID of the video.</param>
        /// <returns>The video data.</returns>
        Task<Video> GetVideoMetaData(int id);
    }
}
