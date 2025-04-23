using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.VideoServices;
using OmegaStreamWebAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly VideoController _videoController;

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

        }

    }
}
