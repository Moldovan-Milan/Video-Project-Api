using Microsoft.AspNetCore.Mvc;
using VideoProjektAspApi.Services;

namespace VideoProjektAspApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserManagerService _userManagerService;

        public UserController(IUserManagerService userManagerService)
        {
            _userManagerService = userManagerService;
        }

        [Route("register")]
        [HttpPost]
        public async Task<IActionResult> Register([FromForm] string username, [FromForm] string email, [FromForm] string password, [FromForm] IFormFile avatar)
        {
            var result = await _userManagerService.RegisterUser(username, email, password, avatar);
            if (result.Succeeded)
                return Ok();

            return BadRequest(result.Errors);
        }

        [Route("login")]
        [HttpPost]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password)
        {
            var token = await _userManagerService.LoginUser(email, password);
            if (token == null)
                return Unauthorized("Invalid email or password.");
            return Ok(token);
        }

        [Route("logout")]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _userManagerService.LogoutUser();
            return Ok();
        }
    }
}

