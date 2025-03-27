using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Models;

namespace OmegaStreamWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private UserManager<User> _userManager;
        private UserService _userService;
        private SignInManager<User> _signInManager;

        public AdminController(UserManager<User> userManager, UserService userService, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _userService = userService;
            _signInManager = signInManager;
        }

        [HttpGet("admin-test")]
        public IActionResult AdminTest()
        {
            return Ok("If you see this, you are an admin user!");
        }

        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> Delete([FromRoute] string userId) 
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
    }
}
