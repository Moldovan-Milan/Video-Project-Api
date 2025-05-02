using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.HttpSys;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using Moq;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.UserServices;
using OmegaStreamWebAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static OmegasStreamApi_UnitTests.Controllers.UserControllerTests;

namespace OmegasStreamApi_UnitTests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IImageService> _imageServiceMock;
        private readonly Mock<ICloudService> _cloudServiceMock;
        private readonly UserController _userController;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _imageServiceMock = new Mock<IImageService>();
            _cloudServiceMock = new Mock<ICloudService>();
            _userController = new TestableUserController(_userServiceMock.Object, _cloudServiceMock.Object, _mapperMock.Object, _imageServiceMock.Object);

            _userController.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        public class TestableUserController : UserController
        {
            public TestableUserController(IUserService userService, ICloudService cloudService, IMapper mapper,
                IImageService imageService)
                : base(userService, cloudService, mapper, imageService)
            {
            }

            protected override IActionResult HandleException(Exception ex, string context)
            {
                return new ObjectResult($"Internal server error: {ex.Message}")
                {
                    StatusCode = 500
                };
            }
        }

        private IFormFile CreateDummyFormFile(string content = "Dummy image content", string fileName = "avatar.jpg")
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(byteArray);
            return new FormFile(stream, 0, byteArray.Length, "avatar", fileName);
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            string userId = "testUser";
            _userServiceMock
                .Setup(x => x.GetUserById(userId))
                .ReturnsAsync((User)null);

            // Act
            var result = await _userController.GetUser(userId);

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            notFoundResult.Value.Should().Be($"Couldn't find user with id: {userId}");
        }

        [Fact]
        public async Task GetUser_ReturnsOk_WhenUserFound()
        {
            // Arrange
            string userId = "testUser";
            var user = new User { Id = userId, UserName = "Test user" };
            _userServiceMock
                .Setup(x => x.GetUserById(userId))
                .ReturnsAsync(user);

            var userDto = new UserDto { Id = userId, UserName = "Test user" };
            _mapperMock
                .Setup(x => x.Map<UserDto>(user))
                .Returns(userDto);

            // Act
            var result = await _userController.GetUser(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(userDto);
        }

        [Fact]
        public async Task GetUser_ReturnsInternalServerError_WhenExceptionThrown()
        {
            // Arrange
            string userId = "testUser";
            _userServiceMock
                .Setup(x => x.GetUserById(userId))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _userController.GetUser(userId);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(500);
            objectResult.Value.Should().Be("Internal server error: Test exception");
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenRegistrationSucceeds()
        {
            // Arrange
            string username = "TestUser";
            string email = "test@example.com";
            string password = "Password123";
            IFormFile avatar = CreateDummyFormFile();

            _userServiceMock
                .Setup(s => s.RegisterUser(username, email, password, It.IsAny<Stream>()))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userController.Register(username, email, password, avatar);

            // Assert
            result.Should().BeOfType<OkResult>();

            _userServiceMock.Verify(s => s.RegisterUser(username, email, password, It.IsAny<Stream>()), Times.Once);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenRegistrationFails()
        {
            // Arrange
            string username = "TestUser";
            string email = "test@example.com";
            string password = "Password123";
            IFormFile avatar = CreateDummyFormFile();

            var identities = IdentityResult.Failed(new IdentityError { Code = "Duplicate", Description = "Email already exists." });

            _userServiceMock
                .Setup(s => s.RegisterUser(username, email, password, It.IsAny<Stream>()))
                .ReturnsAsync(identities);

            // Act
            var result = await _userController.Register(username, email, password, avatar);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequestResult.Value.Should().BeEquivalentTo(identities.Errors);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUserIsNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "Password123";
            bool rememberMe = true;

            _userServiceMock.Setup(s => s.LoginUser(email, password, rememberMe))
                .ReturnsAsync((refreshToken: "dummyToken", user: (User)null));

            // Act
            var result = await _userController.Login(email, password, rememberMe);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().Be("Invalid email or password.");
        }

        [Fact]
        public async Task Login_ReturnsOk_WithCookie_WhenRefreshTokenIsProvided()
        {
            // Arrange
            string email = "test@example.com";
            string password = "Password123";
            bool rememberMe = true;
            string refreshToken = "sampleRefreshToken";
            DateTime now = DateTime.Now;

            var user = new User { Id = "user1", UserName = "TestUser", Created = now};
            var userDto = new UserDto {
                Id = "user1", 
                UserName = "TestUser",
                Avatar = null,
                AvatarId = 0,
                Created = now,
                Email = null,
                FollowersCount = 0,
            };

            _userServiceMock.Setup(s => s.LoginUser(email, password, rememberMe))
                .ReturnsAsync((refreshToken, user));
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _userController.Login(email, password, rememberMe);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            //okResult.Value.Should().BeEquivalentTo(new { userDto });

            var httpContext = _userController.ControllerContext.HttpContext;
            httpContext.Response.Headers.Should().ContainKey("Set-Cookie");
            string cookieHeader = httpContext.Response.Headers["Set-Cookie"];
            cookieHeader.Should().Contain("RefreshToken=" + refreshToken);
        }

        [Fact]
        public async Task Login_ReturnsOk_WithoutCookie_WhenRefreshTokenIsNull()
        {
            // Arrange
            string email = "test@example.com";
            string password = "Password123";
            bool rememberMe = false;

            var user = new User { Id = "user1", UserName = "TestUser" };
            var userDto = new UserDto { Id = "user1", UserName = "TestUser" };

            _userServiceMock.Setup(s => s.LoginUser(email, password, rememberMe))
                .ReturnsAsync((refreshToken: (string)null, user: user));
            _mapperMock.Setup(m => m.Map<UserDto>(user)).Returns(userDto);

            _userController.ControllerContext.HttpContext.Response.Headers.Clear();

            // Act
            var result = await _userController.Login(email, password, rememberMe);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            //okResult.Value.Should().BeEquivalentTo(new { userDto });

            var httpContext = _userController.ControllerContext.HttpContext;
            string cookieHeader = httpContext.Response.Headers["Set-Cookie"];

            cookieHeader.Should().NotContain("RefreshToken");
        }
    
         [Fact]
        public async Task RefreshJwtToken_ReturnsBadRequest_WhenRefreshTokenIsMissing()
        {
          
            var cookies = new Mock<IRequestCookieCollection>();
            cookies.Setup(c => c.ContainsKey(It.IsAny<string>())).Returns(false);
            _userController.ControllerContext.HttpContext.Request.Cookies = cookies.Object;
            // Act
            var result = await _userController.RefreshJwtToken();

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Refresh token is missing.");
        }

        
        [Fact]
        public async Task RefreshJwtToken_ReturnsUnauthorized_WhenLoginWithRefreshTokenFails()
        {
            // Arrange
            var cookies = new Mock<IRequestCookieCollection>();
            cookies.Setup(c => c.ContainsKey("RefreshToken")).Returns(true);
            cookies.Setup(c => c["RefreshToken"]).Returns("oldTokenValue");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Cookies = cookies.Object;
            _userController.ControllerContext.HttpContext = httpContext;

            _userServiceMock
                .Setup(x => x.LogInWithRefreshToken("oldTokenValue"))
                .ReturnsAsync((newRefreshToken: (string)null, user: (User)null));

            // Act
            var result = await _userController.RefreshJwtToken();

            // Assert
            result.Should().BeOfType<UnauthorizedResult>();

            httpContext.Response.Headers["Set-Cookie"].ToString()
                .Should().Contain("RefreshToken=; expires=");
        }

        [Fact]
        public async Task RefreshJwtToken_ReturnsOk_WhenLoginWithRefreshTokenSucceeds()
        {
            // Arrange
            var cookies = new Mock<IRequestCookieCollection>();
            cookies.Setup(c => c.ContainsKey("RefreshToken")).Returns(true);
            cookies.Setup(c => c["RefreshToken"]).Returns("oldTokenValue");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Cookies = cookies.Object;
            _userController.ControllerContext.HttpContext = httpContext;

            var user = new User { Id = "user1", UserName = "TestUser" };
            string newRefreshToken = "newTokenValue";
            var roles = new List<string> { "Role1", "Role2" };

            _userServiceMock
                .Setup(x => x.LogInWithRefreshToken("oldTokenValue"))
                .ReturnsAsync((newRefreshToken, user));
            _userServiceMock
                .Setup(x => x.GetRoles(user.Id))
                .ReturnsAsync(roles);

            var userDto = new UserDto { Id = "user1", UserName = "TestUser" };
            _mapperMock.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

            // Act
            var result = await _userController.RefreshJwtToken();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { user = userDto, roles = roles });

            string cookieHeader = httpContext.Response.Headers["Set-Cookie"];
            cookieHeader.Should().Contain($"RefreshToken={newRefreshToken}");
            cookieHeader.ToLower().Should().Contain("httponly");
            cookieHeader.ToLower().Should().Contain("secure");
            cookieHeader.ToLower().Should().Contain("samesite=strict");
        }
    }
}
