using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Data;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.UserServices;
using System.Text;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IAvatarService _avatarService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IMapper _mapper;

    private readonly byte[] JWT_KEY;
    private readonly string ISSUER;

    public UserService(UserManager<User> userManager, IPasswordHasher<User> passwordHasher,
        SignInManager<User> signInManager, IConfiguration configuration,
        IAvatarService avatarService, IRefreshTokenService refreshTokenService, IMapper mapper)
    {
        _userManager = userManager;
        _passwordHasher = passwordHasher;
        _signInManager = signInManager;
        _configuration = configuration;
        _avatarService = avatarService;
        _refreshTokenService = refreshTokenService;
        _mapper = mapper;

        JWT_KEY = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        ISSUER = _configuration["Jwt:Issuer"]!;
    }

    public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
    {
        if (await _userManager.FindByEmailAsync(email) != null)
        {
            return IdentityResult.Failed(new IdentityError { Description = "Email already exists." });
        }

        string avatarFileName = await _avatarService.SaveAvatarAsync(avatar);
        var user = new User
        {
            UserName = username,
            Email = email,
            AvatarId = (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email))?.AvatarId ?? 0,
            Created = DateTime.UtcNow
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, password);
        return await _userManager.CreateAsync(user);
    }

    public async Task<(string, string, User)> LoginUser(string email, string password, bool rememberMe)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (null, null, null)!;

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, true);
        if (!result.Succeeded) return (null, null, null)!;

        string accessToken = TokenGenerator.GenerateJwtToken(user, JWT_KEY, ISSUER);
        string refreshToken = rememberMe ? await _refreshTokenService.GetOrGenerateRefreshToken(user.Id) : null!;
        return (accessToken, refreshToken, user);
    }

    // Szerintem ez felesleges ide, de még nem törlöm ki
    public async Task LogoutUser()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<(Stream file, string contentType)> GetUserAvatarImage(int id)
    {
        return await _avatarService.GetAvatarAsync(id);
    }

    public async Task<(string?, User?)> GenerateJwtWithRefreshToken(string refreshToken)
    {
        var (isValid, token) = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
        if (!isValid) return (null, null);

        return (TokenGenerator.GenerateJwtToken(token.User, JWT_KEY, ISSUER), token.User);
    }

    public async Task<User?> GetUserById(string id)
    {
        return await _userManager.Users.Include(x => x.Followers).FirstOrDefaultAsync(
            x => x.Id == id);
    }

    public async Task<UserWithVideosDto?> GetUserProfileWithVideos(string userId)
    {
        User user = await _userManager.Users.Include(x => x.Videos).Include(x => x.Followers)
            .FirstOrDefaultAsync(x => x.Id == userId);
        if (user != null)
        {
            UserWithVideosDto userWithVideosDto = _mapper.Map<User, UserWithVideosDto>(user);
            //userWithVideosDto.FollowersCount = user.Followers.Count;
            return userWithVideosDto;
        }
        return null;
    }

    public async Task<List<UserDto?>> GetUsersByName(string name)
    {
        var users = _userManager.Users.Where(x => x.UserName.ToLower().Contains(name.ToLower())).ToList();
        return _mapper.Map<List<UserDto?>>(users);
    }
}
