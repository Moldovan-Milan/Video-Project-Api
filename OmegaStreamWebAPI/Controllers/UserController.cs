using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services;
using System.Security.Claims;
using AutoMapper;
using OmegaStreamServices.Services.UserServices;

namespace OmegaStreamWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IImageService _imageService;
        private readonly IMapper _mapper;



        public UserController(IUserService userManagerService, IImageRepository imageRepository, ICloudService cloudService,
            IMapper mapper, IImageService imageService)
        {
            _userService = userManagerService;
            _mapper = mapper;
            _imageService = imageService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUser(string userId)
        {
            try
            {
                User user = await _userService.GetUserById(userId);
                if (user == null)
                {
                    return NotFound($"Couldn't find user with id: {userId}");
                }
                return Ok(_mapper.Map<UserDto>(user));
            }
            catch (Exception ex)
            {
                return HandleException(ex, "user/id");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllUsers([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            try
            {
                var users = await _userService.GetUsersAsync(pageNumber, pageSize);
                bool hasMore = users.Count == pageSize;
                var userDtos = _mapper.Map<List<UserDto>>(users);
                return Ok(new
                {
                    users = userDtos,
                    hasMore = hasMore
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "all-users");
            }
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
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] bool rememberMe)
        {
            var (refreshToken, user) = await _userService.LoginUser(email, password, rememberMe);

            if (user == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            UserDto userDto = _mapper.Map<User, UserDto>(user);

            if (refreshToken != null)
            {
                var refreshTokenCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(1),
                };
                Response.Cookies.Append("RefreshToken", refreshToken, refreshTokenCookieOptions);
            }

            return Ok(new { userDto });
        }


        [Route("refresh-jwt-token")]
        [HttpGet]
        public async Task<IActionResult> RefreshJwtToken()
        {
            var refreshToken = Request.Cookies["RefreshToken"];

            if (string.IsNullOrEmpty(refreshToken))
            {
                return BadRequest("Refresh token is missing.");
            }

            var (newRefreshToken, user) = await _userService.LogInWithRefreshToken(refreshToken);

            if (newRefreshToken == null || user == null)
            {
                Response.Cookies.Delete("RefreshToken");
                return Unauthorized();
            }

            var refreshTokenCookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(1)
            };


            Response.Cookies.Append("RefreshToken", newRefreshToken, refreshTokenCookieOptions);

            var userRoles = await _userService.GetRoles(user.Id);

            return Ok(new { user = _mapper.Map<UserDto>(user), roles = userRoles });
        }

        [Route("logout")]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _userService.LogoutUser();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            };

            Response.Cookies.Append("AccessToken", "", cookieOptions);
            Response.Cookies.Append("RefreshToken", "", cookieOptions);

            return Ok("Logged out successfully.");
        }

        [Route("profile")]
        [HttpGet]
        [Authorize]
        // TODO: Profilszerkesztéshez esetleg plusz adatotak is elküldeni
        public async Task<IActionResult> Profile([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken == null)
            {
                return Unauthorized("You are not logged in!");
            }

            UserWithVideosDto user = await _userService.GetUserProfileWithVideos(userIdFromToken, pageNumber, pageSize);

            return user == null ? NotFound() : Ok(_mapper.Map<UserWithVideosDto>(user));
        }

        [HttpGet]
        [Route("banner/{id}")]
        public async Task<IActionResult> GetBannerImage(int id)
        {
            try
            {
                (Stream file, string extension) = await _userService.GetBannerAsync(id);
                return File(file, extension);

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("profile/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetUserProfileWithVideos(string id, [FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            UserWithVideosDto user = await _userService.GetUserProfileWithVideos(id, pageNumber, pageSize);
            bool hasMore = user.Videos.Count == pageSize;

            return user == null ? NotFound() : Ok(new
            {
                user = _mapper.Map<UserWithVideosDto>(user),
                hasMore = hasMore
            });
        }

        [Route("avatar/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetAvatarImage(int id)
        {
            try
            {
                (Stream file, string extension) = await _imageService.GetImageStreamByIdAsync("images/avatars", id);
                return File(file, extension);

            }
            catch (Exception ex)
            {
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

        [Authorize]
        [Route("profile/update-username")]
        [HttpPatch]
        public async Task<IActionResult> UpdateUsername([FromQuery] string newName)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken == null)
            {
                return Unauthorized("You are not logged in!");
            }

            User user = await _userService.GetUserById(userIdFromToken);

            if (user == null)
            {
                return NotFound();
            }

            await _userService.UpdateUsernameAsync(user, newName);

            return Ok(_mapper.Map<UserDto>(user));

        }

        [Authorize]
        [HttpPost]
        [Route("profile/set-theme")]
        public async Task<IActionResult> SetTheme([FromForm] string? background, [FromForm] string? primaryColor, [FromForm] string? secondaryColor,
            [FromForm] IFormFile? bannerImage)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userIdFromToken == null)
            {
                return Forbid("You are not logged in!");
            }

            User? user = await _userService.GetUserById(userIdFromToken);

            if (user == null)
            {
                return NotFound();
            }

            bool result;
            if (bannerImage == null)
            {
                result = await _userService.SaveTheme(background, primaryColor, secondaryColor, null, user);
            }
            else
            {
                result = await _userService.SaveTheme(background, primaryColor, secondaryColor, bannerImage.OpenReadStream(), user);
            }

            if (!result)
            {
                return BadRequest("An error happened!");
            }
            return Ok();
        }

        [Authorize]
        [Route("profile/delete-account")]
        [HttpDelete]
        public async Task<IActionResult> DeleteAccount()
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == null)
                {
                    return Unauthorized("You are not logged in!");
                }
                User user = await _userService.GetUserById(userIdFromToken);
                if (user == null)
                {
                    return NotFound();
                }
                if (user.Email == "admin@omegastream.com")
                {
                    return Forbid("You cannot delete the admin account!");
                }
                await _userService.DeleteAccount(userIdFromToken);
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddDays(-1)
                };

                Response.Cookies.Append("AccessToken", "", cookieOptions);
                Response.Cookies.Append("RefreshToken", "", cookieOptions);

                return NoContent();
            }
            catch (Exception ex)
            {
                return HandleException(ex, "delete-account");
            }

        }

        [Route("get-roles/{userId}")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetRoles([FromRoute] string? userId)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var roles = await _userService.GetRoles(userIdFromToken);
                if (userId == null)
                {
                    if (userIdFromToken == null)
                    {
                        return Forbid("You are not logged in!");
                    }
                }
                if (userId == userIdFromToken)
                {
                    return Ok(roles);
                }
                if (!roles.Contains("Admin"))
                {
                    return Unauthorized("You are not authorized!");
                }
                var userRoles = await _userService.GetRoles(userId);
                return Ok(userRoles);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "get-roles");
            }
        }

        [Route("request-verification")]
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> RequestVerification()
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdFromToken == null)
                {
                    return Unauthorized("You are not logged in!");
                }
                var roles = await _userService.GetRoles(userIdFromToken);
                var user = await _userService.GetUserById(userIdFromToken);
                if (roles.Contains("Verified"))
                {
                    return Conflict("You are already verified!");
                }

                if (user.IsVerificationRequested)
                {
                    return Conflict("Verification request already submitted!");
                }

                await _userService.AddVerificationRequest(userIdFromToken);

                return Ok("Verification request submitted successfully.");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "request-verification");
            }
        }

        [Route("{userId}/verification-request/active")]
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CheckForVerificationRequest([FromRoute] string userId)
        {
            try
            {
                var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userIdFromToken == null)
                {
                    return Forbid("You are not logged in!");
                }
                var roles = await _userService.GetRoles(userIdFromToken);
                if (userIdFromToken != userId && !roles.Contains("Admin"))
                {
                    return Forbid("You are not authorized to access this information");
                }
                if (roles.Contains("Verified"))
                {
                    return Conflict("This user is already verified");
                }
                bool hasVerificationRequest = await _userService.HasActiveVerificationRequest(userId);
                return Ok(hasVerificationRequest);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "verification-request/active");
            }
        }

        private IActionResult HandleException(Exception ex, string resourceName)
        {
            return StatusCode(500, new { message = $"There was an error: {ex.Message}" });
        }
    }
}
