using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Services
{
    public class UserManagerService : IUserManagerService
    {
        private readonly UserManager<User> _userManager;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly SignInManager<User> _signInManager;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IFileManagerService _FileManagerService;

        public UserManagerService(UserManager<User> userManager, IPasswordHasher<User> passwordHasher, AppDbContext context, SignInManager<User> signInManager, IConfiguration configuration, IFileManagerService fileManagerService)
        {
            _userManager = userManager;
            _passwordHasher = passwordHasher;
            _context = context;
            _signInManager = signInManager;
            _configuration = configuration;
            _FileManagerService = fileManagerService;
        }

        /// <summary>
        /// Registers a new user with the provided details.
        /// </summary>
        /// <param name="username">The username of the new user.</param>
        /// <param name="email">The email of the new user.</param>
        /// <param name="password">The password of the new user.</param>
        /// <param name="avatar">The avatar image of the new user.</param>
        /// <returns>An IdentityResult indicating the success or failure of the registration.</returns>
        public async Task<IdentityResult> RegisterUser(string username, string email, string password, IFormFile avatar)
        {
            if (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email) != null)
                return IdentityResult.Failed(new IdentityError { Description = "Email already exists." });

            string uniqueFileName = _FileManagerService.GenerateFileName();
            await CreateAvatar(uniqueFileName, avatar);

            User user = new User
            {
                UserName = username,
                Email = email,
                AvatarId = _context.Images.FirstOrDefault(i => i.Path == uniqueFileName).Id,
                Created = DateTime.Now,
                Followers = 0
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            return await _userManager.CreateAsync(user);
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
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("id", user.Id.ToString())
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
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
        private async Task CreateAvatar(string fileName, IFormFile image)
        {
            string imagePath = Path.Combine("images/avatars/", $"{fileName}.png");

            await _context.Images.AddAsync(new Image
            {
                Path = fileName,
                Extension = "png"
            });
            await _context.SaveChangesAsync();
        }
    }
}
