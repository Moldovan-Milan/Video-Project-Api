using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.UserServices;

namespace OmegaStreamWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private UserManager<User> _userManager;
        private IUserService _userService;
        private SignInManager<User> _signInManager;
        private IMapper _mapper;

        public AdminController(UserManager<User> userManager, IUserService userService, SignInManager<User> signInManager, IMapper mapper)
        {
            _userManager = userManager;
            _userService = userService;
            _signInManager = signInManager;
            _mapper = mapper;
        }

        [HttpGet("admin-test")]
        public IActionResult AdminTest()
        {
            return Ok("If you see this, you are an admin user!");
        }

        [HttpPatch("edit-user/{userId}")]
        public async Task<IActionResult> EditUser([FromRoute] string userId, [FromQuery] string username)
        {
            //TODO: Avatar and banner
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            user.UserName = username;

            await _userManager.UpdateAsync(user);
            return NoContent();
        }

        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser([FromRoute] string userId) 
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            //TODO: Logout deleted user
            
            await _userService.DeleteAccount(userId);
            return NoContent();
        }

        [HttpGet("verification-requests")]
        public async Task<IActionResult> GetVerificationRequests([FromQuery] int? pageNumber, [FromQuery] int? pageSize)
        {
            var users = await _userService.GetVerificationRequests(pageNumber, pageSize);
            var userDtos = _mapper.Map<List<UserDto>>(users);

            bool hasMore = users.Count == pageSize;
            return Ok(new
            {
                users = userDtos,
                hasMore = hasMore
            });
        }

        [HttpPost("verify-user/{userId}")]
        public async Task<IActionResult> VerifyUser([FromRoute] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User Not Found!");
            }
            await _userService.VerifyUser(userId);
            return Ok("User verified successfully.");
        }

        [HttpPost("decline-verification/{userId}")]
        public async Task<IActionResult> DeclineVerification([FromRoute] string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User Not Found!");
            }
            await _userService.DeclineVerification(userId);
            return Ok("Verification request declined.");
        }

        //TODO: stop livestream
    }
}
