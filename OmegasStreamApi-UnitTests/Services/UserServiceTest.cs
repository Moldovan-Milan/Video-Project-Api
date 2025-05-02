using AutoMapper;
using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.VideoServices;
using OmegaStreamServices.Services;
using Moq;
using Microsoft.AspNetCore.Http;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamWebAPI.Controllers;

namespace OmegasStreamApi_UnitTests;

public class UserServiceTest
{
    private readonly Mock<IGenericRepository> _repo;
    private readonly Mock<UserManager<User>> _userManager;
    private readonly Mock<SignInManager<User>> _signInManager;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<AppDbContext> _context;
    private readonly Mock<IImageService> _imageService;
    private readonly Mock<IVideoManagementService> _videoManagementService;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository;
    private readonly Mock<ICloudService> _cloudService;
    private readonly Mock<UserService> _userService;
    private readonly UserController _userController;

    public UserServiceTest()
    {
        _repo = new Mock<IGenericRepository>();
        _userManager = new Mock<UserManager<User>>();
        _signInManager = new Mock<SignInManager<User>>();
        _mapper = new Mock<IMapper>();
        _context = new Mock<AppDbContext>();
        _imageService = new Mock<IImageService>();
        _videoManagementService = new Mock<IVideoManagementService>();
        _refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        _cloudService = new Mock<ICloudService>();
        _userService = new Mock<UserService>(_userManager.Object, _signInManager.Object, 
            _mapper.Object, _context.Object, _cloudService.Object, _imageService.Object,
            _videoManagementService.Object, _refreshTokenRepository.Object, _repo.Object);
        _userController = new UserController(_userService.Object,
            _cloudService.Object, _mapper.Object, _imageService.Object);
    }

    //[Fact]
    //public async Task Register_ReturnsBadRequest_WhenRegistrationFails()
    //{
    //    string username = "TestUser";
    //    string password = "TestPassword";
    //    string email = "testemail@email.com";
    //    var avatar = CreateMockFormFile();

    //    var identityResult = IdentityResult.Failed(new IdentityError { Description = "Invalid email format" });

    //    _userService
    //        .Setup(s => s.RegisterUser(username, email, password, It.IsAny<Stream>()))
    //        .ReturnsAsync(identityResult);

    //    var result = await _userController.Register(username, email, password, avatar);

    //    // Assert: az eredménynek BadRequestObjectResult típusúnak kell lennie, és tartalmaznia kell az elvárt hibaleírást
    //    var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
    //    var errors = badRequest.Value.Should().BeAssignableTo<IEnumerable<IdentityError>>().Subject;
    //    errors.Should().ContainSingle(e => e.Description == "Invalid email format");
    //}


    //private IFormFile CreateMockFormFile()
    //{
    //    var content = "Fake image content";
    //    var fileName = "avatar.png";
    //    var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
    //    return new FormFile(stream, 0, stream.Length, "avatar", fileName);
    //}


}
