using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Services.Repositories;

namespace OmegaStreamServices.Services.UserServices
{
    // Dependecies for the service
    public class UserManagerService : IUserManagerService
    {
        private readonly UserManager<User> _userManager;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly SignInManager<User> _signInManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IFileManagerService _FileManagerService;
        private readonly IImageRepository _imageRepository;
        private readonly ICloudService _cloudServices;

        public UserManagerService(UserManager<User> userManager, IPasswordHasher<User> passwordHasher, SignInManager<User> signInManager, AppDbContext context, IConfiguration configuration, IFileManagerService fileManagerService, IImageRepository imageRepository, ICloudService cloudServices
            )
        {
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _signInManager = signInManager;
            _context = context;
            _configuration = configuration;
            _FileManagerService = fileManagerService;
            _imageRepository = imageRepository;
            _cloudServices = cloudServices;

        }


        /// <summary>
        /// Registers a new user with the provided details.
        /// </summary>
        /// <param name="username">The username of the new user.</param>
        /// <param name="email">The email of the new user.</param>
        /// <param name="password">The password of the new user.</param>
        /// <param name="avatar">The avatar image of the new user.</param>
        /// <returns>An IdentityResult indicating the success or failure of the registration.</returns>
        public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
{
            if (await IsEmailTaken(email))
                return IdentityResult.Failed(new IdentityError { Description = "Email already exists." });

            string uniqueFileName = _FileManagerService.GenerateFileName();
            await CreateAvatar(uniqueFileName, avatar);

            Image image = await _imageRepository.FindImageByPath(uniqueFileName);
            var user = new User
            {
                UserName = username,
                Email = email,
                AvatarId = image.Id,
                Created = DateTime.UtcNow,
                Followers = 0,
                //Verified = false
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            return await _userManager.CreateAsync(user);
        }

        private async Task<bool> IsEmailTaken(string email)
        {
            return await _userManager.Users.AnyAsync(u => u.Email == email);
        }

        /// <summary>
        /// Logs in a user with the provided email and password.
        /// </summary>
        /// <param name="email">The email of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>A JWT token if the login is successful, otherwise null.</returns>
        public async Task<string> LoginUser(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                return null;

            var result = await _signInManager.PasswordSignInAsync(user.UserName, password, false, false);
            if (result.Succeeded)
            {
                return GenerateJwtToken(user);
            }
            return null;
        }

        /// <summary>
        /// Logs out the currently logged-in user.
        /// </summary>
        public async Task LogoutUser()
        {
            await _signInManager.SignOutAsync();
        }

        /// <summary>
        /// Generates a JWT token for the specified user.
        /// </summary>
        /// <param name="user">The user for whom the token is generated.</param>
        /// <returns>A JWT token.</returns>
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id.ToString()),
                new Claim("imageId", user.AvatarId.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Issuer"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Creates an avatar image for the user and saves it to the database.
        /// </summary>
        /// <param name="fileName">The unique file name for the avatar image.</param>
        /// <param name="image">The avatar image file.</param>

        private async Task CreateAvatar(string fileName, Stream image)
        {
            await SaveAvatarToCloud(fileName, image);
            await SaveAvatarMetadata(fileName);
        }

        private async Task SaveAvatarToCloud(string fileName, Stream imageStream)
        {
            string imagePath = Path.Combine("images/avatars/", $"{fileName}.png");
            await _cloudServices.UploadToR2(imagePath, imageStream);
        }

        private async Task SaveAvatarMetadata(string fileName)
        {
            var imageEntity = new Image
            {
                Path = fileName,
                Extension = "png"
            };
            await _imageRepository.Add(imageEntity);
        }

        public Task<User> GetUserById(string id)
        {
            
            return _userManager.FindByIdAsync(id)!;
        }

        public async Task<(Stream file, string contentType)> GetUserAvatarImage(int id)
        {
            Image image = await _imageRepository.FindByIdAsync(id);
            if (image == null)
            {
                throw new Exception("Avatar not found on the database");
            }
            return await _cloudServices.GetFileStreamAsync($"images/avatars/{image.Path}.{image.Extension}");
        }
    }
}
