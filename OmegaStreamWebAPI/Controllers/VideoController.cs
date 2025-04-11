using Amazon.Runtime.Internal;
using Amazon.S3;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.VideoServices;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoController : ControllerBase
    {
        private readonly IVideoUploadService _videoUploadService;
        private readonly IVideoStreamService _videoStreamService;
        private readonly ICommentService _commentService;
        private readonly IVideoMetadataService _videoMetadataService;
        private readonly IVideoLikeService _videoLikeService;
        private readonly ISubscriptionRepository _userSubscribeRepository;
        private readonly IVideoViewService _videoViewService;
        private readonly IVideoManagementService _videoManagementService;
        private readonly IEncryptionHelper _encryptionHelper;
        private readonly ILogger<VideoController> _logger;
        private readonly UserManager<User> _userManager;
        private readonly ICommentRepositroy _commentRepository;
        private readonly IUserVideoUploadRepositroy _userVideoUploadRepositroy;
        private readonly List<string> supportedFormats = new List<string> { ".mp4", ".mov" };

        public VideoController(IVideoUploadService videoUploadService, IVideoStreamService videoStreamService, ILogger<VideoController> logger, ICommentService commentService, IVideoMetadataService videoMetadataService, IVideoLikeService videoLikeService, ISubscriptionRepository userSubscribeRepository, IVideoViewService videoViewService, IEncryptionHelper encryptionHelper, IVideoManagementService videoManagementService, UserManager<User> userManager, ICommentRepositroy commentRepository, IUserVideoUploadRepositroy userVideoUploadRepositroy)
        {
            _videoUploadService = videoUploadService;
            _videoStreamService = videoStreamService;
            _logger = logger;
            _commentService = commentService;
            _videoMetadataService = videoMetadataService;
            _videoLikeService = videoLikeService;
            _userSubscribeRepository = userSubscribeRepository;
            _videoViewService = videoViewService;
            _encryptionHelper = encryptionHelper;
            _videoManagementService = videoManagementService;
            _userManager = userManager;
            _commentRepository = commentRepository;
            _userVideoUploadRepositroy = userVideoUploadRepositroy;
        }

        #region Video Stream

        [HttpGet("{id}")]
        public async Task<IActionResult> GetVideo(int id)
        {
            _logger.LogInformation("Fetching video metadata for ID: {Id}", id);
            var video = await _videoMetadataService.GetVideoMetaData(id);
            if (video == null)
            {
                _logger.LogWarning("Video metadata not found for ID: {Id}", id);
                return NotFound();
            }

            string videoKey = $"{video.Path}.m3u8";

            try
            {
                _logger.LogInformation("Fetching video stream for key: {Key}", videoKey);
                var (videoStream, contentType) = await _videoStreamService.GetVideoStreamAsync(videoKey);
                return File(videoStream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "video");
            }
        }

        [HttpGet("segments/{segmentKey}")]
        public async Task<IActionResult> GetVideoSegment(string segmentKey)
        {
            _logger.LogInformation("Fetching video segment for key: {SegmentKey}", segmentKey);
            try
            {
                var (segmentStream, contentType) = await _videoStreamService.GetVideoSegmentAsync(segmentKey).ConfigureAwait(false);
                return File(segmentStream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "segment");
            }
        }
        #endregion

        #region Metadata

        [HttpGet]
        public async Task<IActionResult> GetVideosData([FromQuery]int? pageNumber, [FromQuery] int? pageSize)
        {
            _logger.LogInformation("Fetching all video metadata.");
            var videos = await _videoMetadataService.GetAllVideosMetaData(pageNumber, pageSize, false);
            if (videos == null || videos.Count == 0)
            {
                _logger.LogWarning("No videos found.");
                return NotFound();
            }
            bool hasMore = videos.Count == pageSize;
            _logger.LogInformation("Successfully fetched video metadata.");
            return Ok(new
            {
                videos = videos,
                hasMore = hasMore
            });
        }


        // This could be simplified
        [HttpGet]
        [Route("shorts")]
        public async Task<IActionResult> GetShortsData([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            _logger.LogInformation("Fetching all shorts metadata.");
            var videos = await _videoMetadataService.GetAllVideosMetaData(pageNumber, pageSize, true);
            if (videos == null || videos.Count == 0)
            {
                _logger.LogWarning("No shorts found.");
                return NotFound();
            }
            bool hasMore = videos.Count == pageSize;
            _logger.LogInformation("Successfully fetched video metadata.");
            return Ok(new
            {
                shorts = videos,
                hasMore = hasMore
            });
        }

        [HttpGet("data/{id}")]
        public async Task<IActionResult> GetVideoData(int id)
        {
            _logger.LogInformation("Fetching video metadata for ID: {Id}", id);
            try
            {
                var video = await _videoMetadataService.GetVideoMetaData(id);
                if (video == null)
                {
                    _logger.LogWarning("Video metadata not found for ID: {Id}", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully fetched video metadata for ID: {Id}", id);
                return Ok(video);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "video metadata");
            }
        }

        [HttpGet]
        [Route("get-user-like-subscribe-value/{videoId}")]
        [Authorize]
        public async Task<IActionResult> GetUserLikeAndSubscribe(int videoId){
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var video = await _videoMetadataService.GetVideoMetaData(videoId);
                if (userIdFromToken == null)
                {
                    return Unauthorized();
                }
                // Like
                _logger.LogInformation("Get the value of the user like for user: {UserId}, video: {VideoId}", userIdFromToken, videoId);
                string likeResult = await _videoLikeService.IsUserLikedVideo(userIdFromToken, videoId);
                _logger.LogInformation("Get the subscribe of the user like for user: {UserId}, video: {VideoId}", userIdFromToken, videoId);

                // Subscribe
                bool subscribeResult = await _userSubscribeRepository.IsUserSubscribedToChanel(userIdFromToken, video.UserId);

                return Ok(new { likeResult, subscribeResult });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "user like and subscribe");
            }
        }
        [Route("is-user-subscribed/{followedId}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> IsUserSubscribedToChanel(string followedId)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == null)
                {
                    return BadRequest();
                }
                return Ok(await _userSubscribeRepository.IsUserSubscribedToChanel(userIdFromToken, followedId));

            }
            catch (Exception ex)
            {
                return HandleException(ex, "get subscribe bool");
            }
        }

        [HttpPost("set-user-like/{videoId}")]
        [Authorize]
        public async Task<IActionResult> SetUserLike(int videoId, [FromForm] string value)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == null)
                    return Unauthorized("User id is not valid");
                bool success = await _videoLikeService.UpdateUserLikedVideo(videoId, userIdFromToken, value);
                return Ok(success);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "set-video-like");
            }
        }

        [Authorize]
        [HttpPost]
        [Route("change-subscribe/{chanelId}")]
        public async Task<IActionResult> ChangeSubcribe([FromForm] bool value, string chanelId)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == chanelId)
                {
                    return BadRequest(new { message = "You cannot subscribe to yourself." });
                }
                if (value)
                {
                    await _userSubscribeRepository.SubscribeUserToChanel(userIdFromToken, chanelId);
                }
                else
                {
                    await _userSubscribeRepository.RemoveUserSubscribeFromChanel(userIdFromToken, chanelId);
                }
                return Ok();
            }
            catch (Exception ex)
            {
                return HandleException(ex, "change subscribe");
            }
        }

        [HttpGet("thumbnail/{imageId}")]
        public async Task<IActionResult> GetThumbnailImage(int imageId)
        {
            _logger.LogInformation("Fetching thumbnail image for ID: {ImageId}", imageId);
            try
            {
                var (imageStream, contentType) = await _videoStreamService.GetThumbnailStreamAsync(imageId).ConfigureAwait(false);
                return File(imageStream, contentType);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "thumbnail");
            }
        }

        [HttpGet("search/{searchString}")]
        public async Task<IActionResult> Search(string searchString, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            if (searchString == null)
                return BadRequest("Search is null");
            try
            {
                var videos = await _videoMetadataService.GetVideosByName(searchString, pageNumber, pageSize, false);
                bool hasMore = videos.Count == pageSize;
                return Ok(new
                {
                    videos = videos,
                    hasMore = hasMore,
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "search");
            }
        }
        #endregion

        #region Comments

        [Authorize]
        [HttpPost("write-new-comment")]
        public async Task<IActionResult> WriteNewComment([FromForm] NewCommentDto newComment)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //_logger.LogDebug("User id is: {UserId}, video id is: {VideoId}", userIdFromToken, newComment.VideoId);
                if (userIdFromToken == null)
                {
                    return Unauthorized("You are not logged in!");
                }
                int id = await _commentService.AddNewComment(newComment, userIdFromToken);
                if (id != -1)
                    return Ok(id);
                else
                    return BadRequest("There was some unexpected error :(");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "new-comment");
            }
        }

        

        [Authorize]
        [HttpPatch("edit-comment/{commentId}")]
        public async Task<IActionResult> EditComment([FromRoute] int commentId, [FromBody] string content)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == null)
                {
                    return Unauthorized("You are not logged in!");
                }

                var comment = await _commentRepository.FindByIdAsync(commentId);
                if(comment == null)
                {
                    return NotFound($"Comment with id: {commentId} Not Found");
                }
                var user = await _userManager.FindByIdAsync(userIdFromToken);
                var roles = await _userManager.GetRolesAsync(user);
                if (comment.UserId != userIdFromToken && !roles.Contains("Admin"))
                {
                    return Unauthorized("You are not authorized to edit this comment.");
                }
                comment.Content = content;
                _commentRepository.Update(comment);
                
                return NoContent();
            }
            catch(Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpDelete("delete-comment/{commentId}")]
        public async Task<IActionResult> DeleteComment([FromRoute] int commentId)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == null)
                {
                    return Unauthorized("You are not logged in!");
                }

                var comment = await _commentRepository.FindByIdAsync(commentId);
                if (comment == null)
                {
                    return NotFound($"Comment with id: {commentId} Not Found");
                }
                var user = await _userManager.FindByIdAsync(userIdFromToken);
                var roles = await _userManager.GetRolesAsync(user);
                if (comment.UserId != userIdFromToken && !roles.Contains("Admin"))
                {
                    return Unauthorized("You are not authorized to edit this comment.");
                }
                _commentRepository.Delete(comment);

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }


        #endregion

        #region Video Upload

        [Authorize(Roles = "Verified,Admin")]
        [HttpPost]
        [Route("can-upload")]
        public async Task<IActionResult> CanUploadVideo([FromForm] long videoSize, [FromForm] string fileName, [FromForm] string extension, [FromForm] IFormFile? thumbnail)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized("You are not logged in!");
            }
            if (!supportedFormats.Contains(extension))
            {
                return BadRequest("Unsupported video format.");
            }
            bool response = (await _videoUploadService.CanUploadVideo(videoSize)) && (thumbnail!=null && (await _videoUploadService.CanUploadThumbnail(thumbnail)).Item1);
            if (response)
            {
                await _userVideoUploadRepositroy.AddUserVideoUpload(new UserVideoUpload
                {
                    UserId = userIdFromToken,
                    VideoName = fileName,
                });
            }
            return Ok(response);
        }

        [Authorize(Roles = "Verified,Admin")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadChunk([FromForm] IFormFile chunk, [FromForm] string fileName, [FromForm] int chunkNumber)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized("You are not logged in!");
            }
            if (string.IsNullOrEmpty(fileName) || chunkNumber < 0 || chunk == null)
            {
                _logger.LogWarning("No chunk uploaded for file: {FileName}, chunk number: {ChunkNumber}", fileName, chunkNumber);
                return BadRequest("Invalid file name or chunk number.");
            }

            _logger.LogInformation("Uploading chunk {ChunkNumber} for file: {FileName}", chunkNumber, fileName);
            await using var chunkStream = chunk.OpenReadStream();
            await _videoUploadService.UploadChunk(chunkStream, fileName, chunkNumber).ConfigureAwait(false);
            _logger.LogInformation("Successfully uploaded chunk {ChunkNumber} for file: {FileName}", chunkNumber, fileName);
            return Created("", new { message = "Chunk uploaded successfully." });
        }

        [Authorize(Roles = "Verified,Admin")]
        [HttpPost("assemble")]
        public async Task<IActionResult> AssembleFile([FromForm] string fileName, [FromForm] IFormFile? image, [FromForm] int totalChunks, [FromForm] string title, [FromForm] string? description, [FromForm] string extension)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized("You are not logged in!");
            }

            _logger.LogInformation("Assembling file: {FileName} with {TotalChunks} chunks.", fileName, totalChunks);
            await using var imageStream = image?.OpenReadStream();
            await _videoUploadService.AssembleFile(fileName, imageStream, totalChunks, title, description, extension, userIdFromToken).ConfigureAwait(false);
            _logger.LogInformation("Successfully assembled file: {FileName}", fileName);

            await _userVideoUploadRepositroy.RemoveUserVideoUploadByName(fileName);

            return Created("", new { message = "Video assembled successfully." });
        }

        [HttpGet("get-supported-formats")]
        public IActionResult GetSupportedFormats()
        {
            return Ok(supportedFormats);
        }

        #endregion Video Upload

        #region View Validation
        [HttpPost("add-video-view")]
        public async Task<IActionResult> AddVideoView([FromQuery] int videoId, [FromQuery] string? userId)
        {
            if (videoId <= 0)
            {
                return BadRequest("Invalid video view data.");
            }

            try
            {
                if(userId == null)
                {
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                    var encryptedIp = _encryptionHelper.Encrypt(ipAddress);

                    var videoView = new VideoView
                    {
                        UserId = null,
                        VideoId = videoId,
                        ViewedAt = DateTime.UtcNow,
                        IpAddressHash = encryptedIp
                    };
                    if (await _videoViewService.ValidateView(videoView))
                    {
                        return Created("", new {message = "Video View added successfully, by Guest"});
                    }
                }
                else
                {
                    var videoView = new VideoView
                    {
                        UserId=userId,
                        VideoId=videoId,
                        ViewedAt = DateTime.UtcNow
                    };
                    if(await _videoViewService.ValidateView(videoView))
                    {
                        return Created("", new { message = "Video View added successfully, by Logged In User" });
                    }
                }
                return BadRequest("Failed to validate view");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }


        #endregion

        #region Watch History
        [Authorize]
        [HttpGet("watch-history")]
        public async Task<IActionResult> WatchHistory([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized();
            }

            var videoViewHistory = await _videoViewService.GetUserViewHistory(userIdFromToken, pageNumber, pageSize);

            bool hasMore = videoViewHistory.Count == pageSize;
            return Ok(new
            {
                videoViews = videoViewHistory,
                hasMore = hasMore
            });
        }
        #endregion

        #region Manage Videos
        [Authorize]
        [HttpDelete("delete/{videoId}")]
        public async Task<IActionResult> DeleteVideo([FromRoute] int videoId)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userIdFromToken);
                var roles = await _userManager.GetRolesAsync(user);
                var video = await _videoMetadataService.GetVideoMetaData(videoId);
                if (video == null)
                {
                    return NotFound("Video not found.");
                }
                if (userIdFromToken == null || (video.UserId != userIdFromToken && !roles.Contains("Admin")))
                {
                    return Unauthorized("You are not authorized to delete this video.");
                }
                    
                await _videoManagementService.DeleteVideoWithAllRelations(videoId);
                return NoContent();
            }
            catch (Exception ex) {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }

        [Authorize]
        [HttpPatch("update/{videoId}")]
        public async Task<IActionResult> EditVideo(
            [FromRoute] int videoId,
            [FromForm] string? title,
            [FromForm] string? description,
            [FromForm] IFormFile? image)
        {
            try
            {
                string? userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdFromToken))
                {
                    return Unauthorized("Invalid user token.");
                }

                var video = await _videoMetadataService.GetVideoMetaData(videoId);
                if (video == null)
                {
                    return NotFound("Video not found.");
                }
                var user = await _userManager.FindByIdAsync(userIdFromToken);
                var roles = await _userManager.GetRolesAsync(user);

                if (video.UserId != userIdFromToken && !roles.Contains("Admin"))
                {
                    return Forbid();
                }

                await _videoManagementService.EditVideo(videoId, title, description, image);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Video not found.");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        #endregion

        private IActionResult HandleException(Exception ex, string resourceName)
        {
            if (ex is AmazonS3Exception amazonS3Ex)
            {
                _logger.LogError(amazonS3Ex, "{ResourceName} not found.", resourceName);
                return NotFound(new { message = $"{resourceName} not found, error: {amazonS3Ex.Message}" });
            }

            _logger.LogError(ex, "There was an error: {Message}", ex.Message);
            return StatusCode(500, new { message = $"There was an error: {ex.Message}" });
        }
    }
}