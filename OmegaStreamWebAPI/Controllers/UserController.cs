﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Services.UserServices;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services;

namespace OmegaStreamWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserManagerService _userManagerService;
       

        public UserController(IUserManagerService userManagerService, IImageRepository imageRepository, ICloudService cloudService)
        {
            _userManagerService = userManagerService;

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

        [Route("profile/{id}")]
        [HttpGet]
        //[Authorize]
        public async Task<IActionResult> Profile(string id)
        {
            User user = await _userManagerService.GetUserById(id);
            if (user == null)
            {
                return NotFound();
            }
            user.PasswordHash = String.Empty;
            return Ok(user);
        }

        [Route("avatar/{id}")]
        [HttpGet]
        public async Task<IActionResult> GetAvatarImage(int id)
        {
            try
            {
                (Stream file, string extension) = await _userManagerService.GetUserAvatarImage(id);
                return File(file, $"image/png");

            }
            catch (Exception ex){
                return BadRequest(ex.Message);
            }
        }
    }
}

