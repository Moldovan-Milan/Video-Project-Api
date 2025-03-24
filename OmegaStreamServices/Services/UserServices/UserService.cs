using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Data;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.UserServices;
using System.Linq;
using System.Text;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;
    private readonly IImageRepository _imageRepository;
    private readonly IUserThemeRepository _userThemeRepository;
    private readonly ICloudService _cloudService;
    private readonly IImageService _imageService;

    private readonly byte[] JWT_KEY;
    private readonly string ISSUER;

    public UserService(UserManager<User> userManager,
        SignInManager<User> signInManager, IConfiguration configuration,
        IRefreshTokenService refreshTokenService, IMapper mapper, AppDbContext context, IImageRepository imageRepository, IUserThemeRepository userThemeRepository, ICloudService cloudService, IImageService imageService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _refreshTokenService = refreshTokenService;
        _mapper = mapper;

        JWT_KEY = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!);
        ISSUER = _configuration["Jwt:Issuer"]!;
        _context = context;

        _imageRepository = imageRepository;
        _userThemeRepository = userThemeRepository;
        _cloudService = cloudService;
        _imageService = imageService;
    }

    public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
    {
        //if (await _userManager.FindByEmailAsync(email) != null)
        //{
        //    return IdentityResult.Failed(new IdentityError { Description = "Email already exists." });
        //}

        string avatarFileName = await _imageService.SaveImage("images/avatars", avatar);

        var avatarImage = await _imageRepository.FindImageByPath(avatarFileName);
        var user = new User
        {
            UserName = username,
            Email = email,
            AvatarId = avatarImage != null ? avatarImage.Id : 0,
            Created = DateTime.UtcNow,
        };
        //user.PasswordHash = _passwordHasher.HashPassword(user, password);
        return await _userManager.CreateAsync(user, password);
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
        return await _imageService.GetImageStreamByIdAsync("images/avatars", id);
        //return await _avatarService.GetAvatarAsync(id);
    }

    public async Task<(string?, User?)> GenerateJwtWithRefreshToken(string refreshToken)
    {
        var (isValid, token) = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
        if (!isValid) return (null, null);

        return (TokenGenerator.GenerateJwtToken(token.User, JWT_KEY, ISSUER), token.User);
    }

    public async Task<User?> GetUserById(string id)
    {
        return await _userManager.Users.Include(x => x.Followers).Include(x => x.UserTheme).FirstOrDefaultAsync(
            x => x.Id == id);
    }

    public async Task<User?> GetUserWithFollowersById(string id)
    {
        return await _userManager.Users.Include(x => x.Following).FirstOrDefaultAsync(
            x => x.Id == id);
    }

    public async Task<UserWithVideosDto?> GetUserProfileWithVideos(string userId, int? pageNumber, int? pageSize)
    {
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

        User? user = await _userManager.Users
            .Include(x => x.Videos)
            .Include(x => x.Followers)
            .Include(x=>x.UserTheme)
            .FirstOrDefaultAsync(x => x.Id == userId);

        if (user != null)
        {
            var orderedVideos = user.Videos
                .OrderByDescending(v => v.Created)
                .Skip((pageNumber.Value - 1) * pageSize.Value)
                .Take(pageSize.Value)
                .ToList();

            UserWithVideosDto userWithVideosDto = _mapper.Map<User, UserWithVideosDto>(user);
            userWithVideosDto.Videos = _mapper.Map<List<VideoDto>>(orderedVideos);

            return userWithVideosDto;
        }

        return null;
    }


    public async Task<List<UserDto?>> GetUsersByName(string name, int? pageNumber, int? pageSize)
    {
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
        var users = _userManager.Users
            .Where(x => x.UserName.ToLower().Contains(name.ToLower()))
            .Skip((pageNumber.Value - 1) * pageSize.Value)
            .Take(pageSize.Value)
            .ToList();
        return _mapper.Map<List<UserDto?>>(users);
    }

    public async Task<bool> UpdateUsername(User user,string newName)
    {
        var result = await _userManager.SetUserNameAsync(user, newName);
        await _userManager.UpdateNormalizedUserNameAsync(user);
        await _context.SaveChangesAsync();
        return result.Succeeded;
    }

    public async Task<bool> SaveTheme(string? background, string? textColor, Stream? bannerImage, User user)
    {
        try
        {
            var userTheme = user.UserTheme != null
                ? user.UserTheme
                : new UserTheme();

            if (background != "null")
                userTheme.Background = background;

            if (textColor != "null")
                userTheme.TextColor = textColor;

            if (bannerImage != null)
            {
                if (userTheme.BannerId != null)
                {
                    var existingImage = await _imageRepository.FindByIdAsync(userTheme.BannerId.Value);
                    if (existingImage != null)
                    {
                        if (!await _imageService.ReplaceImage("images/banner", existingImage.Path, bannerImage))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    string fileName = await SaveBanner(bannerImage);
                    if (string.IsNullOrEmpty(fileName))
                        return false;

                    var image = await _imageRepository.FindImageByPath(fileName);
                    if (image != null)
                    {
                        userTheme.BannerId = image.Id;
                    }
                }
            }
            _userThemeRepository.Update(userTheme);
            user.UserTheme = userTheme;
            await _userManager.UpdateAsync(user);


            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return false;
        }
    }


    private async Task<string> SaveBanner(Stream bannerStream)
    {
        return await _imageService.SaveImage("images/banner", bannerStream);
    }

    public async Task<(Stream file, string contentType)> GetBannerAsync(int bannerId)
    {
        return await _imageService.GetImageStreamByIdAsync("images/banner", bannerId);
    }
}
