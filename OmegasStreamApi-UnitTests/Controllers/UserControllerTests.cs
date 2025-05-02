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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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
            _userController = new UserController(_userServiceMock.Object, _cloudServiceMock.Object, _mapperMock.Object, _imageServiceMock.Object);
        }

        [Fact]
        public async Task GetUser_ReturnsOk_WhenUserExists()
        {
            string userId = "123";
            var user = new User { Id = userId, UserName = "Test User" };
            var userDto = new UserDto { Id = userId, UserName = "Test User" };

            _userServiceMock.Setup(x => x.GetUserById(userId)).ReturnsAsync(user);
            _mapperMock.Setup(x => x.Map<UserDto>(user)).Returns(userDto);

            var result = await _userController.GetUser(userId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnDto = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userDto.Id, returnDto.Id);
        }

        [Fact]
        public async Task GetUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            string userId = "999";
            _userServiceMock.Setup(x => x.GetUserById(userId)).ReturnsAsync((User)null);

            var result = await _userController.GetUser(userId);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        [Fact]
        public async Task GetUser_ReturnsHandledException_WhenExceptionOccurs()
        {
            var userId = "error";
            _userServiceMock.Setup(s => s.GetUserById(userId))
                .ThrowsAsync(new Exception("Db is down"));

            var result = await _userController.GetUser(userId);

            result.Should().BeOfType<ObjectResult>()
                .Which.StatusCode.Should().Be(500);

            var value = ((ObjectResult)result).Value;
            value.Should().BeEquivalentTo(new
            {
                message = "There was an error: Db is down"
            });
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenRegistrationSucceeds()
        {
            string userName = "TestUser";
            string password = "TestPassword";
            string email = "testemail@email.com";
            var avatar = CreateMockFormFile();

            _userServiceMock.Setup(x => x.RegisterUser(userName, email, password, It.IsAny<Stream>()))
                .ReturnsAsync(IdentityResult.Success);

            var result = await _userController.Register(userName, email, password, avatar);
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenRegistrationFails()
        {
            string username = "TestUser";
            string password = "TestPassword";
            string email = "testemail@email.com";
            var avatar = CreateMockFormFile();

            var identityResult = IdentityResult.Failed(new IdentityError { Description = "Invalid email format" });

            _userServiceMock.Setup(s => s.RegisterUser(username, email, password, It.IsAny<Stream>()))
                       .ReturnsAsync(identityResult);

            var result = await _userController.Register(username, email, password, avatar);

            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var errors = badRequest.Value.Should().BeAssignableTo<IEnumerable<IdentityError>>().Subject;

            errors.Should().ContainSingle(e => e.Description == "Invalid email format");
        }

        [Fact]
        public async Task Login_ReturnsOk_WithUserDto_WhenLoginSucceeds()
        {
            string email = "testemail@email.com";
            string password = "TestPassword";
            bool rememberMe = true;

            var mockUser = new User
            {
                Id = "123",
                UserName = "TestUser",
                Email = email
            };

            string refreshToken = "exampleRefreshToken";

            _userServiceMock.Setup(x => x.LoginUser(email, password, rememberMe))
                .ReturnsAsync((refreshToken, mockUser));

            _mapperMock.Setup(x => x.Map<UserDto>(mockUser))
                .Returns(new UserDto
                {
                    Id = mockUser.Id,
                    UserName = mockUser.UserName,
                    Email = mockUser.Email
                });

            var httpContext = new DefaultHttpContext();
            httpContext.Response.Body = new MemoryStream();
            _userController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            var result = await _userController.Login(email, password, rememberMe);

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            dynamic response = okResult.Value;
        }

        private IFormFile CreateMockFormFile(string content = "fake image", string fileName = "avatar.png")
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            return new FormFile(stream, 0, stream.Length, "avatar", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };
        }

    }
}
