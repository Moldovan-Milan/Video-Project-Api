using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.VideoServices;
using OmegaStreamWebAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static OmegasStreamApi_UnitTests.Controllers.VideoControllerTests;

namespace OmegasStreamApi_UnitTests.Controllers
{
    public class VideoControllerTests
    {
        private readonly Mock<IGenericRepository> _genericRepoMock;
        private readonly Mock<IVideoUploadService> _videoUploadServiceMock;
        private readonly Mock<ICommentService> _commentServiceMock;
        private readonly Mock<IVideoMetadataService> _videoMetadataServiceMock;
        private readonly Mock<IVideoLikeService> _videoLikeServiceMock;
        private readonly Mock<ISubscriptionRepository> _subscriptionServiceMock;
        private readonly Mock<IVideoViewService> _videoViewServiceMock;
        private readonly Mock<IVideoManagementService> _videoManagementServiceMock;
        private readonly Mock<ILogger<VideoController>> _loggerMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<IUserVideoUploadRepositroy> _mockUserVideoUploadRepository;
        private readonly Mock<IEncryptionHelper> _encryptionHelperMock;
        private readonly TestableVideoController _videoController;

        public VideoControllerTests()
        {
            _genericRepoMock = new Mock<IGenericRepository>();
            _videoUploadServiceMock = new Mock<IVideoUploadService>();
            _commentServiceMock = new Mock<ICommentService>();
            _videoMetadataServiceMock = new Mock<IVideoMetadataService>();
            _videoLikeServiceMock = new Mock<IVideoLikeService>();
            _subscriptionServiceMock = new Mock<ISubscriptionRepository>();
            _videoViewServiceMock = new Mock<IVideoViewService>();
            _videoManagementServiceMock = new Mock<IVideoManagementService>();
            _loggerMock = new Mock<ILogger<VideoController>>();
            _userManagerMock = new Mock<UserManager<User>>(
                new Mock<IUserStore<User>>().Object,
                null, null, null, null, null, null, null, null);
            _mockUserVideoUploadRepository = new Mock<IUserVideoUploadRepositroy>();
            _encryptionHelperMock = new Mock<IEncryptionHelper>();

            _videoController = new TestableVideoController(_genericRepoMock.Object,
                _encryptionHelperMock.Object, _videoUploadServiceMock.Object, _commentServiceMock.Object,
                _videoMetadataServiceMock.Object, _videoLikeServiceMock.Object, _subscriptionServiceMock.Object,
                _videoViewServiceMock.Object, _videoManagementServiceMock.Object, _loggerMock.Object, _userManagerMock.Object,
                _mockUserVideoUploadRepository.Object);

        }

        public class TestableVideoController : VideoController
        {
            public TestableVideoController(
                IGenericRepository genericRepo,
                IEncryptionHelper encryptionHelper,
                IVideoUploadService videoUploadService,
                ICommentService commentService,
                IVideoMetadataService videoMetadataService,
                IVideoLikeService videoLikeService,
                ISubscriptionRepository subscriptionService,
                IVideoViewService videoViewService,
                IVideoManagementService videoManagementService,
                ILogger<VideoController> logger,
                UserManager<User> userManager,
                IUserVideoUploadRepositroy userVideoUploadRepository)
                : base(videoUploadService, logger, commentService, videoMetadataService, videoLikeService,
                      subscriptionService, videoViewService, encryptionHelper, videoManagementService, 
                      userManager, userVideoUploadRepository, genericRepo)
            { }

            protected override IActionResult HandleException(Exception ex, string context)
            {
                return new ObjectResult(new { Error = $"Handled exception in {context}" })
                {
                    StatusCode = 500
                };
            }
        }

        [Fact]
        public async Task GetVideosData_ReturnsNotFound_WhenVideosIsNull()
        {
            int? pageNumber = 1;
            int? pageSize = 10;

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, false))
                .ReturnsAsync((List<VideoDto>)null);

            // Act
            var result = await _videoController.GetVideosData(pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetVideosData_ReturnsNotFound_WhenVideosIsEmpty()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 10;

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, false))
                .ReturnsAsync(new List<VideoDto>());

            // Act
            var result = await _videoController.GetVideosData(pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetVideosData_ReturnsOkResult_WithVideosAndHasMoreTrue()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 3;
            var videos = new List<VideoDto>
            {
                new VideoDto { Id = 1, Title = "Video 1" },
                new VideoDto { Id = 2, Title = "Video 2" },
                new VideoDto { Id = 3, Title = "Video 3" }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, false))
                .ReturnsAsync(videos);

            // Act
            var actionResult = await _videoController.GetVideosData(pageNumber, pageSize);
            var okResult = actionResult as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response.GetType();

            var videosProp = responseType.GetProperty("videos");
            videosProp.Should().NotBeNull();
            var videosValue = (IEnumerable<VideoDto>)videosProp.GetValue(response);
            videosValue.Should().BeEquivalentTo(videos);

            var hasMoreProp = responseType.GetProperty("hasMore");
            hasMoreProp.Should().NotBeNull();
            var hasMoreValue = (bool)hasMoreProp.GetValue(response);
            hasMoreValue.Should().BeTrue();
        }

        [Fact]
        public async Task GetVideosData_ReturnsOkResult_WithVideosAndHasMoreFalse()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 5;
            var videos = new List<VideoDto>
            {
                new VideoDto { Id = 1, Title = "Video 1" },
                new VideoDto { Id = 2, Title = "Video 2" }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, false))
                .ReturnsAsync(videos);

            // Act
            var actionResult = await _videoController.GetVideosData(pageNumber, pageSize);
            var okResult = actionResult as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response.GetType();

            var videosProp = responseType.GetProperty("videos");
            videosProp.Should().NotBeNull();
            var videosValue = (IEnumerable<VideoDto>)videosProp.GetValue(response);
            videosValue.Should().BeEquivalentTo(videos);

            var hasMoreProp = responseType.GetProperty("hasMore");
            hasMoreProp.Should().NotBeNull();
            var hasMoreValue = (bool)hasMoreProp.GetValue(response);
            hasMoreValue.Should().BeFalse();
        }


        [Fact]
        public async Task GetShortsData_ReturnsNotFound_WhenShortsIsNull()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 10;

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, true))
                .ReturnsAsync((List<VideoDto>)null);

            // Act
            var result = await _videoController.GetShortsData(pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetShortsData_ReturnsNotFound_WhenShortsIsEmpty()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 10;

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, true))
                .ReturnsAsync(new List<VideoDto>());

            // Act
            var result = await _videoController.GetShortsData(pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetShortsData_ReturnsOkResult_WithShortsAndHasMoreTrue()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 3;

            var shorts = new List<VideoDto>
            {
                new VideoDto { Id = 1, Title = "Short 1" },
                new VideoDto { Id = 2, Title = "Short 2" },
                new VideoDto { Id = 3, Title = "Short 3" }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, true))
                .ReturnsAsync(shorts);

            // Act
            var actionResult = await _videoController.GetShortsData(pageNumber, pageSize);
            var okResult = actionResult as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response.GetType();

            var shortsProp = responseType.GetProperty("shorts");
            shortsProp.Should().NotBeNull();
            var shortsValue = (IEnumerable<VideoDto>)shortsProp.GetValue(response);
            shortsValue.Should().BeEquivalentTo(shorts);

            var hasMoreProp = responseType.GetProperty("hasMore");
            hasMoreProp.Should().NotBeNull();
            var hasMoreValue = (bool)hasMoreProp.GetValue(response);
            hasMoreValue.Should().BeTrue();
        }

        [Fact]
        public async Task GetShortsData_ReturnsOkResult_WithShortsAndHasMoreFalse()
        {
            // Arrange
            int? pageNumber = 1;
            int? pageSize = 5;

            var shorts = new List<VideoDto>
            {
                new VideoDto { Id = 1, Title = "Short 1" },
                new VideoDto { Id = 2, Title = "Short 2" }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetAllVideosMetaData(pageNumber, pageSize, true))
                .ReturnsAsync(shorts);

            // Act
            var actionResult = await _videoController.GetShortsData(pageNumber, pageSize);
            var okResult = actionResult as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response.GetType();

            var shortsProp = responseType.GetProperty("shorts");
            shortsProp.Should().NotBeNull();
            var shortsValue = (IEnumerable<VideoDto>)shortsProp.GetValue(response);
            shortsValue.Should().BeEquivalentTo(shorts);

            var hasMoreProp = responseType.GetProperty("hasMore");
            hasMoreProp.Should().NotBeNull();
            var hasMoreValue = (bool)hasMoreProp.GetValue(response);
            hasMoreValue.Should().BeFalse();
        }

        [Fact]
        public async Task GetVideoData_ReturnsNotFound_WhenVideoIsNull()
        {
            // Arrange
            int id = 123;
            _videoMetadataServiceMock
                .Setup(x => x.GetVideoMetaData(id))
                .ReturnsAsync((VideoDto)null);

            // Act
            var result = await _videoController.GetVideoData(id);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetVideoData_ReturnsOkResult_WhenVideoExists()
        {
            // Arrange
            int id = 1;
            var videoMetadata = new VideoDto { Id = id, Title = "Sample Video" };
            _videoMetadataServiceMock
                .Setup(x => x.GetVideoMetaData(id))
                .ReturnsAsync(videoMetadata);

            // Act
            var result = await _videoController.GetVideoData(id);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(videoMetadata);
        }

        [Fact]
        public async Task GetVideoData_ReturnsErrorResult_WhenExceptionIsThrown()
        {
            // Arrange
            int id = 789;
            var exception = new Exception("Test exception");
            _videoMetadataServiceMock
                .Setup(x => x.GetVideoMetaData(id))
                .ThrowsAsync(exception);

            var result = await _videoController.GetVideoData(id);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            dynamic value = objectResult.Value;
            ((string)value.Error).Should().Be("Handled exception in video metadata");
        }

        [Fact]
        public async Task GetVideoData_LogsWarning_WhenVideoNotFound()
        {
            // Arrange
            int id = 999;
            _videoMetadataServiceMock
                .Setup(x => x.GetVideoMetaData(id))
                .ReturnsAsync((VideoDto)null);

            // Act
            var result = await _videoController.GetVideoData(id);

            // Assert
            result.Should().BeOfType<NotFoundResult>();

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Fetching video metadata for ID: {id}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains($"Video metadata not found for ID: {id}")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    
        [Fact]
        public async Task GetUserLikeAndSubscribe_ReturnsUnauthorized_WhenUserNotFoundInToken()
        {
            // Arrange
            int videoId = 1;

            var identity = new ClaimsIdentity();
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetVideoMetaData(videoId))
                .ReturnsAsync(new VideoDto { Id = videoId, UserId = "videoOwner" });

            // Act
            var result = await _videoController.GetUserLikeAndSubscribe(videoId);

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();
        }

        [Fact]
        public async Task GetUserLikeAndSubscribe_ReturnsNotFound_WhenVideoIsNull()
        {
            // Arrange
            int videoId = 2;
            string userId = "user123";

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetVideoMetaData(videoId))
                .ReturnsAsync((VideoDto)null);

            // Act
            var result = await _videoController.GetUserLikeAndSubscribe(videoId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>()
                 .Which.Value.Should().Be("Video not found.");
        }

        [Fact]
        public async Task GetUserLikeAndSubscribe_ReturnsOkResult_WithLikeAndSubscribeData()
        {
            // Arrange
            int videoId = 1;
            string userId = "user123";
            var videoMetadata = new VideoDto { Id = videoId, UserId = "videoOwner" };

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetVideoMetaData(videoId))
                .ReturnsAsync(videoMetadata);
            _videoLikeServiceMock
                .Setup(s => s.IsUserLikedVideo(userId, videoId))
                .ReturnsAsync("Liked");
            _genericRepoMock
                .Setup(r => r.AnyAsync<Subscription>(It.IsAny<System.Linq.Expressions.Expression<Func<Subscription, bool>>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _videoController.GetUserLikeAndSubscribe(videoId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value;
            var responseType = response.GetType();

            var likeResultProp = responseType.GetProperty("likeResult");
            likeResultProp.Should().NotBeNull();
            var likeResultValue = (string)likeResultProp.GetValue(response);
            likeResultValue.Should().Be("Liked");

            var subscribeResultProp = responseType.GetProperty("subscribeResult");
            subscribeResultProp.Should().NotBeNull();
            var subscribeResultValue = (bool)subscribeResultProp.GetValue(response);
            subscribeResultValue.Should().BeTrue();
        }

        [Fact]
        public async Task GetUserLikeAndSubscribe_ReturnsErrorResult_WhenExceptionIsThrown()
        {
            // Arrange
            int videoId = 1;
            string userId = "user123";

            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var exception = new Exception("Test exception");
            _videoMetadataServiceMock
                .Setup(s => s.GetVideoMetaData(videoId))
                .ThrowsAsync(exception);

            // Act
            var result = await _videoController.GetUserLikeAndSubscribe(videoId);

            // Assert
            var errorResult = result.Should().BeOfType<ObjectResult>().Subject;
            errorResult.StatusCode.Should().Be(500);
            dynamic value = errorResult.Value;
            ((string)value.Error).Should().Be("Handled exception in user like and subscribe");
        }

        [Fact]
        public async Task Search_ReturnsBadRequest_WhenSearchStringIsNull()
        {
            // Arrange
            string searchString = null;
            int? pageNumber = 1;
            int? pageSize = 10;

            // Act
            var result = await _videoController.Search(searchString, pageNumber, pageSize);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().Be("Search is null");
        }

        [Fact]
        public async Task Search_ReturnsNotFound_WhenVideosIsNull()
        {
            // Arrange
            string searchString = "test";
            int? pageNumber = 1;
            int? pageSize = 10;

            _videoMetadataServiceMock
                .Setup(s => s.GetVideosByName(searchString, pageNumber, pageSize, false))
                .ReturnsAsync((List<VideoDto>)null);

            // Act
            var result = await _videoController.Search(searchString, pageNumber, pageSize);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Search_ReturnsOkResult_WithVideosAndHasMoreTrue()
        {
            // Arrange
            string searchString = "example";
            int? pageNumber = 1;
            int? pageSize = 3;
            var videos = new List<VideoDto>
            {
                new VideoDto { Id = 1, Title = "Video 1" },
                new VideoDto { Id = 2, Title = "Video 2" },
                new VideoDto { Id = 3, Title = "Video 3" }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetVideosByName(searchString, pageNumber, pageSize, false))
                .ReturnsAsync(videos);

            // Act
            var actionResult = await _videoController.Search(searchString, pageNumber, pageSize);
            var okResult = actionResult as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response.GetType();

            var videosProp = responseType.GetProperty("videos");
            videosProp.Should().NotBeNull();
            var videosValue = (IEnumerable<VideoDto>)videosProp.GetValue(response);
            videosValue.Should().BeEquivalentTo(videos);

            var hasMoreProp = responseType.GetProperty("hasMore");
            hasMoreProp.Should().NotBeNull();
            var hasMoreValue = (bool)hasMoreProp.GetValue(response);
            hasMoreValue.Should().BeTrue();
        }

        [Fact]
        public async Task Search_ReturnsOkResult_WithVideosAndHasMoreFalse()
        {
            // Arrange
            string searchString = "example";
            int? pageNumber = 1;
            int? pageSize = 5;
            var videos = new List<VideoDto>
            {
                new VideoDto { Id = 10, Title = "Test Video 1" },
                new VideoDto { Id = 11, Title = "Test Video 2" }
            };

            _videoMetadataServiceMock
                .Setup(s => s.GetVideosByName(searchString, pageNumber, pageSize, false))
                .ReturnsAsync(videos);

            // Act
            var actionResult = await _videoController.Search(searchString, pageNumber, pageSize);
            var okResult = actionResult as OkObjectResult;

            // Assert
            okResult.Should().NotBeNull();
            okResult.StatusCode.Should().Be(200);

            var response = okResult.Value;
            var responseType = response.GetType();

            var videosProp = responseType.GetProperty("videos");
            videosProp.Should().NotBeNull();
            var videosValue = (IEnumerable<VideoDto>)videosProp.GetValue(response);
            videosValue.Should().BeEquivalentTo(videos);

            var hasMoreProp = responseType.GetProperty("hasMore");
            hasMoreProp.Should().NotBeNull();
            var hasMoreValue = (bool)hasMoreProp.GetValue(response);
            hasMoreValue.Should().BeFalse();
        }

        [Fact]
        public async Task CanUploadVideo_ReturnsUnauthorized_WhenUserIdNotFound()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };
            _videoController.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await _videoController.CanUploadVideo(
                videoSize: 1000,
                fileName: "test",
                extension: "mp4",
                thumbnail: null);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                  .Which.Value.Should().Be("You are not logged in!");
        }

        [Fact]
        public async Task CanUploadVideo_ReturnsBadRequest_WhenUnsupportedVideoFormat()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            var thumbnail = new FormFile(new MemoryStream(new byte[10]), 0, 10, "thumb", "thumb.jpg");

            // Act
            var result = await _videoController.CanUploadVideo(
                videoSize: 2000,
                fileName: "test",
                extension: "wmv", // Unsupported video format
                thumbnail: thumbnail);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>()
                  .Which.Value.Should().Be("Unsupported video format.");
        }

        [Fact]
        public async Task CanUploadVideo_ReturnsOkTrue_WhenUploadConditionsAreMet()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _videoUploadServiceMock
                .Setup(x => x.CanUploadVideo(It.IsAny<long>()))
                .ReturnsAsync(true);

            _videoUploadServiceMock
                .Setup(x => x.CanUploadThumbnail(It.IsAny<IFormFile>()))
                .ReturnsAsync((true, ""));

            var dummyThumbnail = new FormFile(new MemoryStream(new byte[100]), 0, 100, "thumb", "thumb.jpg");

            // Act
            var result = await _videoController.CanUploadVideo(
                videoSize: 5000,
                fileName: "video",
                extension: "mp4",
                thumbnail: dummyThumbnail);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(true);

            _mockUserVideoUploadRepository.Verify(x => x.AddUserVideoUpload(It.Is<UserVideoUpload>(uv =>
                uv.UserId == "user123" && uv.VideoName == "video")), Times.Once);
        }

        [Fact]
        public async Task CanUploadVideo_ReturnsOkFalse_WhenUploadConditionsAreNotMet()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _videoUploadServiceMock
                .Setup(x => x.CanUploadVideo(It.IsAny<long>()))
                .ReturnsAsync(true);

            // Act
            var result = await _videoController.CanUploadVideo(
                videoSize: 4000,
                fileName: "video",
                extension: "mp4",
                thumbnail: null);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(false);

            _mockUserVideoUploadRepository.Verify(x => x.AddUserVideoUpload(It.IsAny<UserVideoUpload>()), Times.Never);
        }

        [Fact]
        public async Task UploadChunk_ReturnsUnauthorized_WhenUserIdNotFound()
        {
            // Arrange
            var context = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity())
            };
            _videoController.ControllerContext = new ControllerContext { HttpContext = context };

            // Act
            var result = await _videoController.UploadChunk(
                chunk: new FormFile(new MemoryStream(new byte[1]), 0, 1, "chunk", "chunk.part0"),
                fileName: "sample",
                chunkNumber: 1);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>()
                .Which.Value.Should().Be("You are not logged in!");
        }

        [Fact]
        public async Task UploadChunk_ReturnsBadRequest_WhenInvalidInput()
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            // Invalid Result: filename is empty
            var invalidResult1 = await _videoController.UploadChunk(
                chunk: new FormFile(new MemoryStream(new byte[1]), 0, 1, "chunk", "chunk.part0"),
                fileName: "",
                chunkNumber: 1);
            invalidResult1.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Invalid file name or chunk number.");

            // Invalid result: chunk number is negative
            var invalidResult2 = await _videoController.UploadChunk(
                chunk: new FormFile(new MemoryStream(new byte[1]), 0, 1, "chunk", "chunk.part0"),
                fileName: "sample",
                chunkNumber: -1);
            invalidResult2.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Invalid file name or chunk number.");

            // Invalid result: chunk is null
            var invalidResult3 = await _videoController.UploadChunk(
                chunk: null,
                fileName: "sample",
                chunkNumber: 1);
            invalidResult3.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Invalid file name or chunk number.");
        }

        [Fact]
        public async Task UploadChunk_ReturnsCreated_WhenValidInput()
        {
            // Arrange
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            byte[] dummyData = new byte[] { 1, 2, 3, 4 };
            var stream = new MemoryStream(dummyData);
            var formFile = new FormFile(stream, 0, dummyData.Length, "chunk", "chunk.part0");

            _videoUploadServiceMock
                .Setup(x => x.UploadChunk(It.IsAny<Stream>(), "sample", 2))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _videoController.UploadChunk(
                chunk: formFile,
                fileName: "sample",
                chunkNumber: 2);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            _videoUploadServiceMock.Verify(x => x.UploadChunk(It.IsAny<Stream>(), "sample", 2), Times.Once);

            createdResult.Value.Should().BeEquivalentTo(new { message = "Chunk uploaded successfully." });

        }

        [Fact]
        public async Task AssembleFile_ReturnsUnauthorized_WhenUserIdNotFound()
        {
            // Arrange
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity())
                }
            };

            // Act
            var result = await _videoController.AssembleFile("video.mp4", null, 5, "Test Title", "Test description", "mp4");

            // Assert: UnauthorizedObjectResult az elvárt üzenettel.
            result.Should().BeOfType<UnauthorizedObjectResult>()
                  .Which.Value.Should().Be("You are not logged in!");
        }

        [Fact]
        public async Task AssembleFile_ReturnsCreated_WhenValidInput_WithNonNullImage()
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            byte[] dummyImageData = new byte[] { 1, 2, 3, 4, 5 };
            var imageStream = new MemoryStream(dummyImageData);
            var formFile = new FormFile(imageStream, 0, dummyImageData.Length, "image", "image.jpg");

            _videoUploadServiceMock
                .Setup(x => x.AssembleFile(
                    It.Is<string>(f => f == "video"),
                    It.IsAny<Stream>(),
                    It.Is<int>(ch => ch == 5),
                    It.Is<string>(t => t == "Test Title"),
                    It.Is<string>(d => d == "Test description"),
                    It.Is<string>(ext => ext == "mp4"),
                    It.Is<string>(uid => uid == "user123")))
                .Returns(Task.CompletedTask);

            _mockUserVideoUploadRepository
                .Setup(x => x.RemoveUserVideoUploadByName("video"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _videoController.AssembleFile("video", formFile, 5, "Test Title", "Test description", "mp4");

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(new { message = "Video assembled successfully." });

            _videoUploadServiceMock.Verify(x => x.AssembleFile("video", It.IsAny<Stream>(), 5, "Test Title", "Test description", "mp4", "user123"), Times.Once);
            _mockUserVideoUploadRepository.Verify(x => x.RemoveUserVideoUploadByName("video"), Times.Once);
        }

        [Fact]
        public async Task AssembleFile_ReturnsCreated_WhenValidInput_WithNullImage()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            _videoUploadServiceMock.Setup(x => x.AssembleFile(
                It.Is<string>(f => f == "video"),
                null,
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(uid => uid == "user123")))
                .Returns(Task.CompletedTask);

            _mockUserVideoUploadRepository.Setup(x => x.RemoveUserVideoUploadByName("video"))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _videoController.AssembleFile("video", null, 3, "Title", "Desc", "mp4");

            // Assert
            var createdResult = result.Should().BeOfType<CreatedResult>().Subject;
            createdResult.StatusCode.Should().Be(201);
            createdResult.Value.Should().BeEquivalentTo(new { message = "Video assembled successfully." });

            _videoUploadServiceMock.Verify(x => x.AssembleFile("video", null, 3, "Title", "Desc", "mp4", "user123"), Times.Once);
            _mockUserVideoUploadRepository.Verify(x => x.RemoveUserVideoUploadByName("video"), Times.Once);
        }

        [Fact]
        public async Task AssembleFile_ThrowsException_WhenAssembleFails()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user123") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };

            _videoUploadServiceMock.Setup(x => x.AssembleFile(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ThrowsAsync(new Exception("Assembly failed"));

            // Act
            Func<Task> action = async () => await _videoController.AssembleFile("video.mp4", null, 4, "Title", "Desc", "mp4");

            // Assert
            await action.Should().ThrowAsync<Exception>().WithMessage("Assembly failed");
        }
        
        [Fact]
        public async Task EditVideo_ReturnsUnauthorized_WhenUserTokenInvalid()
        {
            // Arrange
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            };

            // Act
            var result = await _videoController.EditVideo(1, "New Title", "New Description", null);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be("Invalid user token.");
        }

        [Fact]
        public async Task EditVideo_ReturnsNotFound_WhenVideoNotFound()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            _videoMetadataServiceMock.Setup(x => x.GetVideoMetaData(It.IsAny<int>()))
                .ReturnsAsync((VideoDto)null);

            // Act
            var result = await _videoController.EditVideo(10, "Title", "Desc", null);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be("Video not found.");
        }

        [Fact]
        public async Task EditVideo_ReturnsForbid_WhenUserNotOwnerAndNotAdmin()
        {
            // Arrange
            var tokenUserId = "user1";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, tokenUserId) };
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            var video = new VideoDto { Id = 2, UserId = "user2" };
            _videoMetadataServiceMock.Setup(x => x.GetVideoMetaData(2)).ReturnsAsync(video);

            var user = new User { Id = tokenUserId };
            _userManagerMock.Setup(x => x.FindByIdAsync(tokenUserId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Verified" });

            // Act
            var result = await _videoController.EditVideo(2, "Title", "Desc", null);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task EditVideo_ReturnsNoContent_WhenEditSuccessful_ForOwner()
        {
            var tokenUserId = "user1";
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, tokenUserId) };
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            var video = new VideoDto { Id = 3, UserId = tokenUserId };
            _videoMetadataServiceMock.Setup(x => x.GetVideoMetaData(3)).ReturnsAsync(video);

            var user = new User { Id = tokenUserId };
            _userManagerMock.Setup(x => x.FindByIdAsync(tokenUserId)).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Verified" });

            _videoManagementServiceMock.Setup(x => x.EditVideo(30, "Updated Title", "Updated Desc", null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _videoController.EditVideo(3, "Updated Title", "Updated Desc", null);

            // Assert
            result.Should().BeOfType<NoContentResult>();
            _videoManagementServiceMock.Verify(x => x.EditVideo(3, "Updated Title", "Updated Desc", null), Times.Once);
        }

        [Fact]
        public async Task EditVideo_ReturnsNotFound_WhenKeyNotFoundExceptionThrown()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            _videoMetadataServiceMock.Setup(x => x.GetVideoMetaData(It.IsAny<int>()))
                .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _videoController.EditVideo(999, "Title", "Desc", null);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be("Video not found.");
        }

        [Fact]
        public async Task EditVideo_ReturnsForbid_WhenUnauthorizedAccessExceptionThrown()
        {
            // Arrange
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, "user1") };
            _videoController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")) }
            };

            var video = new VideoDto { Id = 2, UserId = "user1" };
            _videoMetadataServiceMock.Setup(x => x.GetVideoMetaData(2)).ReturnsAsync(video);

            var user = new User { Id = "user1" };
            _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);
            _userManagerMock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Verified" });

            _videoManagementServiceMock.Setup(x => x.EditVideo(2, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IFormFile>()))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _videoController.EditVideo(2, "Title", "Desc", null);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }
    }
}
