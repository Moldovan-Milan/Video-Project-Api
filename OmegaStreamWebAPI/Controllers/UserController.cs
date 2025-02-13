using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Services.UserServices;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services;
using System.Security.Claims;
using OmegaStreamServices.Dto;
using AutoMapper;

namespace OmegaStreamWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userManagerService;
        private IAvatarService _avatarService;
        private readonly IMapper _mapper;
       

        public UserController(IUserService userManagerService, IImageRepository imageRepository, ICloudService cloudService,
            IMapper mapper, IAvatarService avatarService)
        {
            _userManagerService = userManagerService;
            _mapper = mapper;
            _avatarService = avatarService;
        }

        [Route("register")]
        [HttpPost]
        public async Task<IActionResult> Register([FromForm] string username, [FromForm] string email, [FromForm] string password, [FromForm] IFormFile avatar)
        {
            IdentityResult result;
            using (var avatarStream = avatar.OpenReadStream())
            {
                result = await _userManagerService.RegisterUser(username, email, password, avatarStream);
            }
            if (result.Succeeded)
                return Ok();

            return BadRequest(result.Errors);
        }

        [Route("login")]
        [HttpPost]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, 
            [FromForm] bool rememberMe)
        {
            var (token, refreshToken) = await _userManagerService.LoginUser(email, password, rememberMe);
            if (token == null)
                return Unauthorized("Invalid email or password.");
            if (rememberMe)
            {
                return Ok(new {token, refreshToken });
            }
            
            return Ok(token);
        }

        [Route("refresh-jwt-token")]
        [HttpPost]
        public async Task<IActionResult> RefreshJwtToken([FromForm] string refreshToken)
        {
            if (refreshToken == null)
            {
                return BadRequest("Refresh token is null");
            }
            string newToken = await _userManagerService.GenerateJwtWithRefreshToken(refreshToken);
            if (newToken == null)
            {
                return Forbid();
            }
            return Ok(newToken);
        }

        [Route("logout")]
        [HttpPost]
        public async Task<IActionResult> Logout([FromForm] string refreshToken)
        {
            await _userManagerService.LogoutUser();
            return Ok();
        }

        [Route("profile")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken == null)
            {
                return Forbid("You are not logged in!");
            }

            User user = await _userManagerService.GetUserById(userIdFromToken);
            if (user == null)
            {
                return NotFound();
            }
            
            UserDto userDto = _mapper.Map<UserDto>(user);

            return Ok(userDto);
        }

        [Route("profile/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetUserProfileWithVideos(string id)
        {
            UserWithVideosDto user = await _userManagerService.GetUserProfileWithVideos(id);
            if (user == null)
            {
                return NotFound();
            }
            return Ok(user);
        }

        [Route("avatar/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetAvatarImage(int id)
        {
            try
            {
                (Stream file, string extension) = await _avatarService.GetAvatarAsync(id);
                return File(file, extension);

            }
            catch (Exception ex){
                return BadRequest(ex.Message);
            }
        }
    }
}

