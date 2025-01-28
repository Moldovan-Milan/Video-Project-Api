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
        Task<(Stream videoStream, string contentType)> GetVideoStreamAsync(string videoKey);
        Task<(Stream segmentStream, string contentType)> GetVideoSegmentAsync(string segmentKey);

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
        Task<VideoDto> GetVideoMetaData(int id);
        Task<(Stream imageStream, string contentType)> GetStreamAsync(int imageId, string path);

        /// <summary>
        /// Gets the video liked by user
        /// </summary
        /// <param name="userId">The ID of the user</param>
        /// <param name="videoId">The ID of the video</param>
        /// <returns>'none', if the user not liked, else 'liked' or 'disliked'</returns>
        Task<string> IsUserLikedVideo(string userId, int videoId);

        /// <summary>
        /// Update the like value of the user
        /// </summary>
        /// <param name="videoId">The id of the Video</param>
        /// <param name="userId">The id of the user</param>
        /// <param name="likeValue">The value of the like: 'none', 'like', 'dislike'</param>
        /// <returns>Whether the change was successfull</returns>
        Task<bool> UpdateUserLikedVideo(int videoId, string userId, string likeValue);

        Task<List<Video>> GetVideosByName(string name);
    }
}
