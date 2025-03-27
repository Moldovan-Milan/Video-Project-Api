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

        public AdminController(UserManager<User> userManager, UserService userService)
        {
            _userManager = userManager;
            _userService = userService;
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
            await _userService.DeleteAccount(userId);
            return NoContent();
        }
    }
}
