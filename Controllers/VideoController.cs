using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VideoProjektAspApi.Services;

namespace VideoProjektAspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IVideoUploadService _videoUploadService;
        private readonly IVideoStreamService _videoStreamService;

        public VideoController(IVideoUploadService videoUploadService, IVideoStreamService videoStreamService)
        {
            _videoUploadService = videoUploadService;
            _videoStreamService = videoStreamService;
        }

        /// <summary>
        /// Gets the video by its ID and streams it.
        /// </summary>
        /// <param name="id">The ID of the video.</param>
        /// <returns>The video stream or a 404 Not Found response if the video does not exist.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            var video = await _videoStreamService.GetVideoData(id);
            if (video == null)
                return NotFound();

            var videoStream = _videoStreamService.StreamVideo(video);
            return CreateVideoStreamResponse(videoStream, video.Extension);
        }

        /// <summary>
        /// Gets all video data.
        /// </summary>
        /// <returns>A list of all videos or a 404 Not Found response if no videos exist.</returns>
        [HttpGet]
        public async Task<IActionResult> GetVideosData()
        {
            var videos = await _videoStreamService.GetAllVideosData();
            return videos == null ? NotFound() : Ok(videos);
        }

        /// <summary>
        /// Gets the video data by its ID.
        /// </summary>
        /// <param name="id">The ID of the video.</param>
        /// <returns>The video data or a 404 Not Found response if the video does not exist.</returns>
        [HttpGet("data/{id}")]
        public async Task<IActionResult> GetVideoData(int id)
        {
            var video = await _videoStreamService.GetVideoData(id);
            return video == null ? NotFound() : Ok(video);
        }

        /// <summary>
        /// Gets the thumbnail image for a video by its ID.
        /// </summary>
        /// <param name="imageId">The ID of the image.</param>
        /// <returns>The thumbnail image stream or a 404 Not Found response if the image does not exist.</returns>
        [HttpGet("thumbnail/{imageId}")]
        public async Task<IActionResult> GetThumbnailImage(int imageId)
        {
            var imageStream = await _videoStreamService.GetThumbnailImage(imageId);
            return imageStream == null ? NotFound() : File(imageStream, $"image/{imageStream.Name.Split('.').Last()}");
        }

        /// <summary>
        /// Uploads a video chunk.
        /// </summary>
        /// <param name="chunk">The video chunk to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="chunkNumber">The chunk number.</param>
        /// <returns>A 201 Created response if the chunk is uploaded successfully.</returns>
        [Authorize]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk, [FromForm] string fileName, [FromForm] int chunkNumber)
        {
            if (chunk == null || chunk.Length == 0)
                return BadRequest("No chunk uploaded.");

            await _videoUploadService.UploadChunk(chunk, fileName, chunkNumber);
            return Created();
        }

        /// <summary>
        /// Assembles the video chunks into a single file.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="image">The thumbnail image.</param>
        /// <param name="totalChunks">The total number of chunks.</param>
        /// <param name="title">The title of the video.</param>
        /// <param name="extension">The file extension of the video.</param>
        /// <param name="userId">The ID of the user who uploaded the video.</param>
        /// <returns>A 201 Created response if the video is assembled successfully.</returns>
        [Authorize]
        [HttpPost("assemble")]
        public async Task<IActionResult> AssembleFile([FromForm] string fileName, [FromForm] IFormFile image, [FromForm] int totalChunks, [FromForm] string title, [FromForm] string extension, [FromForm] string userId)
        {
            await _videoUploadService.AssembleFile(fileName, image, totalChunks, title, extension, userId);
            return Created();
        }

        /// <summary>
        /// Creates a video stream response with range processing if requested.
        /// </summary>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="extension">The file extension of the video.</param>
        /// <returns>The video stream response.</returns>
        private IActionResult CreateVideoStreamResponse(FileStream videoStream, string extension)
        {
            string range = Request.Headers.Range;
            return string.IsNullOrEmpty(range)
                ? File(videoStream, $"video/{extension}")
                : File(videoStream, $"video/{extension}", enableRangeProcessing: true);
        }
    }
}
