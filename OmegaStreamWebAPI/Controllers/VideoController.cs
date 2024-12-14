using Amazon.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Services.VideoServices;

namespace OmegaStreamWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IVideoUploadService _videoUploadService;
        private readonly IVideoStreamService _videoStreamService;

        public VideoController(IVideoUploadService videoUploadService, IVideoStreamService videoStreamService, IVideoStreamService videoStreamService2)
        {
            _videoUploadService = videoUploadService;
            _videoStreamService = videoStreamService2;
        }

        /// <summary>
        /// Gets the video by its ID and streams it.
        /// </summary>
        /// <param name="id">The ID of the video.</param>
        /// <returns>The video stream or a 404 Not Found response if the video does not exist.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            var video = await _videoStreamService.GetVideoMetaData(id);
            if (video == null)
                return NotFound();

            string videoKey = $"{video.Path}.m3u8";

            try
            {
                // Videó adatfolyam lekérése a service segítségével
                var (videoStream, contentType) = await _videoStreamService.GetVideoStreamAsync(videoKey);

                // Stream átadása a kliensnek
                return File(videoStream, contentType, enableRangeProcessing: true);
            }
            catch (AmazonS3Exception ex)
            {
                return NotFound(new { message = $"Nem található a videó: {videoKey}, hiba: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Hiba történt: {ex.Message}" });
            }
        }

        [HttpGet("segments/{segmentKey}")]
        public async Task<IActionResult> GetVideoSegment(string segmentKey)
        {
            try
            {
                // Videó szegmens adatfolyam lekérése a service segítségével
                var (segmentStream, contentType) = await _videoStreamService.GetVideoSegmentAsync(segmentKey);

                // Szegmens stream átadása a kliensnek
                return File(segmentStream, contentType, enableRangeProcessing: true);
            }
            catch (AmazonS3Exception ex)
            {
                return NotFound(new { message = $"Nem található a szegmens: {segmentKey}, hiba: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Hiba történt: {ex.Message}" });
            }
        }


        /// <summary>
        /// Gets all video data.
        /// </summary>
        /// <returns>A list of all videos or a 404 Not Found response if no videos exist.</returns>
        [HttpGet]
        public async Task<IActionResult> GetVideosData()
        {
            var videos = await _videoStreamService.GetAllVideosMetaData();
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
            try
            {
                var video = await _videoStreamService.GetVideoMetaData(id);
                return video == null ? NotFound() : Ok(video);
            }
            catch(AmazonS3Exception ex)
            {
                return NotFound(new { message = $"Hiba történt: ${ex.Message}" });
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Gets the thumbnail image for a video by its ID.
        /// </summary>
        /// <param name="imageId">The ID of the image.</param>
        /// <returns>The thumbnail image stream or a 404 Not Found response if the image does not exist.</returns>
        [HttpGet("thumbnail/{imageId}")]
        public async Task<IActionResult> GetThumbnailImage(int imageId)
        {
            try
            {
                (Stream imageStream, string contentType) = await _videoStreamService.GetStreamAsync(imageId, "thumbnails");
                return File(imageStream, contentType);
            }
            catch (AmazonS3Exception ex)
            {
                return NotFound(new { message = $"Error: ${ex.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"Error {ex.Message}" });
            }
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

            using (var chunkStream = chunk.OpenReadStream())
            {
                await _videoUploadService.UploadChunk(chunkStream, fileName, chunkNumber);
            }
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

            using (var imageStream = image.OpenReadStream())
            {
                await _videoUploadService.AssembleFile(fileName, imageStream, totalChunks, title, extension, userId);

            }
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
            string range = Request.Headers.Range!;
            return string.IsNullOrEmpty(range)
                ? File(videoStream, $"video/{extension}")
                : File(videoStream, $"video/{extension}", enableRangeProcessing: true);
        }
    }
}
