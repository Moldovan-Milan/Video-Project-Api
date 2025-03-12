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
        private readonly IUserService _userService;
        private IAvatarService _avatarService;
        private readonly IMapper _mapper;
       

        public UserController(IUserService userManagerService, IImageRepository imageRepository, ICloudService cloudService,
            IMapper mapper, IAvatarService avatarService)
        {
            _userService = userManagerService;
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
                result = await _userService.RegisterUser(username, email, password, avatarStream);
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
            var (token, refreshToken, user) = await _userService.LoginUser(email, password, rememberMe);
            if (token == null)
                return Unauthorized("Invalid email or password.");
            UserDto userDto = _mapper.Map<User, UserDto>(user);
            if (rememberMe)
            {
                return Ok(new {token, refreshToken, userDto});
            }
            
            return Ok(new {token, userDto});
        }

        [Route("refresh-jwt-token")]
        [HttpPost]
        public async Task<IActionResult> RefreshJwtToken([FromForm] string refreshToken)
        {
            if (refreshToken == null)
            {
                return BadRequest("Refresh token is null");
            }
            var (newToken, user) = await _userService.GenerateJwtWithRefreshToken(refreshToken);
            if (newToken == null)
            {
                return Forbid();
            }
            UserDto userDto = _mapper.Map<UserDto>(user);
            return Ok(new { newToken, userDto});
        }

        [Route("logout")]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _userService.LogoutUser();
            return Ok();
        }

        [Route("profile")]
        [HttpGet]
        [Authorize]
        // TODO: Profilszerkesztéshez esetleg plusz adatotak is elküldeni
        public async Task<IActionResult> Profile()
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken == null)
            {
                return Forbid("You are not logged in!");
            }

            User user = await _userService.GetUserById(userIdFromToken);

            return user == null ? NotFound() : Ok(_mapper.Map<UserDto>(user));
        }

        [Route("profile/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetUserProfileWithVideos(string id, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            UserWithVideosDto user = await _userService.GetUserProfileWithVideos(id, pageNumber, pageSize);
            bool hasMore = user.Videos.Count == pageSize;

            return user == null ? NotFound() : Ok(new 
            { 
                user = user,
                hasMore = hasMore 
            });
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

        [Authorize]
        [Route("{id}/following")]
        [HttpGet]
        public async Task<IActionResult> GetUserFollowedChannels(
            string id,
            [FromQuery] int? videoPage,
            [FromQuery] int? videoCount,
            [FromQuery] int? pageNumber,
            [FromQuery] int? pageSize)
        {
            User user = await _userService.GetUserWithFollowersById(id);
            if (user == null)
            {
                return NotFound();
            }

            var followedIds = user.Following.Select(f => f.FollowedUserId).ToList();

            pageNumber = pageNumber ?? 1;
            pageSize = pageSize ?? 30;
            if (pageNumber <= 0)
            {
                pageNumber = 1;
            }
            if (pageSize <= 0)
            {
                pageSize = 30;
            }

            var paginatedFollowedIds = followedIds
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            var followed = new List<UserWithVideosDto>();
            foreach (string userId in paginatedFollowedIds)
            {
                followed.Add(await _userService.GetUserProfileWithVideos(userId, videoPage, videoCount));
            }
            bool hasMore = followed.Count == pageSize;

            var response = new
            {
                users = followed,
                hasMore = hasMore
            };

            return Ok(response);
        }


        [HttpGet("search/{searchString}")]
        public async Task<IActionResult> SearchUser(string searchString, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            if (searchString == null)
                return BadRequest("Search string is null");
            try
            {
                var users = await _userService.GetUsersByName(searchString, pageNumber, pageSize);
                bool hasMore = users.Count == pageSize;
                return Ok(new
                {
                    users = users,
                    hasMore = hasMore
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "search");
            }
        }

        private IActionResult HandleException(Exception ex, string resourceName)
        {
            return StatusCode(500, new { message = $"There was an error: {ex.Message}" });
        }
    }
}

