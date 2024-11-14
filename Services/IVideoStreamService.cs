using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Services
{
    public interface IVideoStreamService
    {
        /// <summary>
        /// Gets all video data from the database.
        /// </summary>
        /// <returns>A list of all videos.</returns>
        Task<List<Video>> GetAllVideosData();

        /// <summary>
        /// Gets the thumbnail image for a video by its ID.
        /// </summary>
        /// <param name="id">The ID of the image.</param>
        /// <returns>A FileStream of the thumbnail image.</returns>
        Task<FileStream> GetThumbnailImage(int id);

        /// <summary>
        /// Gets the video data by its ID.
        /// </summary>
        /// <param name="id">The ID of the video.</param>
        /// <returns>The video data.</returns>
        Task<Video> GetVideoData(int id);

        /// <summary>
        /// Streams the video file.
        /// </summary>
        /// <param name="video">The video to be streamed.</param>
        /// <returns>A FileStream of the video.</returns>
        FileStream StreamVideo(Video video);
    }
}
