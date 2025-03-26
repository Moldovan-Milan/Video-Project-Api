using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace OmegaStreamWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        [HttpGet("admin-test")]
        public IActionResult AdminTest()
        {
            return Ok("If you see this, you are an admin user!");
        }
    }
}
