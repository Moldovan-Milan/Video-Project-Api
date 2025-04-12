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
using OmegaStreamServices.Services.VideoServices;
using System.Text;

public class UserService : IUserService
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;
    private readonly IImageRepository _imageRepository;
    private readonly IUserThemeRepository _userThemeRepository;
    private readonly IImageService _imageService;
    private readonly IVideoManagementService _videoManagementService;

    private readonly IRefreshTokenRepository _refreshTokenRepository;

    private readonly ICloudService _cloudService;
    private readonly TokenGenerator _tokenGenerator;


    public UserService(UserManager<User> userManager, IPasswordHasher<User> passwordHasher,
        SignInManager<User> signInManager, IConfiguration configuration,

        IMapper mapper,
        AppDbContext context,
        IImageRepository imageRepository, IUserThemeRepository userThemeRepository, ICloudService cloudService, IImageService imageService,
        IVideoManagementService videoManagementService,
        TokenGenerator tokenGenerator, IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
        _mapper = mapper;

        _context = context;

        _imageRepository = imageRepository;
        _userThemeRepository = userThemeRepository;
        _cloudService = cloudService;
        _imageService = imageService;
        _videoManagementService = videoManagementService;

        _tokenGenerator = tokenGenerator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
    {

        string avatarFileName = await _imageService.SaveImage("images/avatars", avatar);
        var avatarImage = await _imageRepository.FindImageByPath(avatarFileName);
        var user = new User
        {
            UserName = username,
            Email = email,
            AvatarId = avatarImage != null ? avatarImage.Id : 0,
            Created = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
        }

        return result;

    }

    public async Task<(string refreshToken, User)> LoginUser(string email, string password, bool rememberMe)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
            return (null, null)!;

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, true);
        if (!result.Succeeded) return (null, null)!;


        //string accessToken = await _tokenGenerator.GenerateJwtToken(user.Id);
        string refreshToken = rememberMe ? await GenerateRefreshToken(user.Id) : null!;
        return (refreshToken, user);
    }

    public async Task LogoutUser()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<(Stream file, string contentType)> GetUserAvatarImage(int id)
    {
        return await _imageService.GetImageStreamByIdAsync("images/avatars", id);
        //return await _avatarService.GetAvatarAsync(id);
    }



    public async Task<List<User>> GetUsersAsync(int? pageNumber, int? pageSize)
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
        return await _userManager.Users
            .Include(x => x.Avatar)
            .Skip((pageNumber.Value - 1) * pageSize.Value)
            .Take(pageSize.Value)
            .ToListAsync();
    }

    public async Task<User?> GetUserById(string id)
    {
        return await _userManager.Users
            .Include(x => x.Avatar)
            .Include(x => x.Followers)
            .Include(x => x.UserTheme)
            .FirstOrDefaultAsync(x => x.Id == id);
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
            .ThenInclude(v => v.Thumbnail)
            .Include(x => x.Followers)
            .Include(x => x.UserTheme)
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

    public async Task<IdentityResult> UpdateUsernameAsync(User user, string newName)
    {
        var result = await _userManager.SetUserNameAsync(user, newName);
        if (!result.Succeeded)
            return result;

        result = await _userManager.UpdateAsync(user);
        return result;
    }


    public async Task<bool> SaveTheme(string? background, string? primaryColor, string? secondaryColor, Stream? bannerImage, User user)
    {
        try
        {
            var userTheme = user.UserTheme != null
                ? user.UserTheme
                : new UserTheme();

            if (background != "null")
                userTheme.Background = background;

            if (primaryColor != "null")
                userTheme.PrimaryColor = primaryColor;

            if (secondaryColor != "null")
                userTheme.SecondaryColor = secondaryColor;

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
    public async Task DeleteAccount(string userId)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null) return;

                var videos = _context.Videos.Where(x => x.UserId == userId);
                foreach (var video in videos)
                {
                    try
                    {
                        await _videoManagementService.DeleteVideoWithAllRelations(video.Id);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to delete video {video.Id}. Error: {ex.Message}");
                    }
                }

                _context.Comments.RemoveRange(_context.Comments.Where(x => x.UserId == userId));
                _context.Subscriptions.RemoveRange(_context.Subscriptions.Where(x => x.FollowerId == userId || x.FollowedUserId == userId));
                _context.VideoLikes.RemoveRange(_context.VideoLikes.Where(x => x.UserId == userId));
                _context.VideoViews.RemoveRange(_context.VideoViews.Where(x => x.UserId == userId));

                var chatIds = _context.UserChats.Where(x => x.User1Id == userId || x.User2Id == userId)
                    .Select(x => x.Id).ToList();
                _context.ChatMessages.RemoveRange(_context.ChatMessages.Where(x => chatIds.Contains(x.UserChatId)));
                _context.UserChats.RemoveRange(_context.UserChats.Where(x => x.User1Id == userId || x.User2Id == userId));

                //_context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(x => x.UserId == userId));

                if (user.Avatar != null && user.Avatar.Path != "default_avatar")
                {
                    try
                    {
                        var avatar = await _imageRepository.FindByIdAsync(user.AvatarId);
                        await _cloudService.DeleteFileAsync($"images/avatars/{user.Avatar.Path}.{user.Avatar.Extension}");
                        _imageRepository.Delete(avatar);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to delete user avatar. Error: {ex.Message}");
                    }
                }

                await _userManager.DeleteAsync(user);
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Error: Transaction rolled back. Reason: {ex.Message}");
                throw;
            }
        }
    }


    public async Task<List<string>> GetRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        var roles = (await _userManager.GetRolesAsync(user)).ToList();
        return roles;
    }

    public async Task<(string? newRefreshToken, User? user)> LogInWithRefreshToken(string refreshToken)
    {
        var refreshTokenObj = await _refreshTokenRepository.GetByToken(refreshToken);
        if (refreshTokenObj == null)
            return (null, null);

        if (!ValidateToken(refreshTokenObj))
        {
            return (null, null);
        }

        User? user = await _userManager.FindByIdAsync(refreshTokenObj.UserId);
        if (user == null)
            return (null, null);

        var newRefreshToken = await _tokenGenerator.GenerateRefreshToken(user.Id);
        _refreshTokenRepository.Delete(refreshTokenObj);
        await _refreshTokenRepository.Add(newRefreshToken);

        await _signInManager.SignInAsync(user, true);

        return (newRefreshToken.Token, user);
    }

    private async Task<string> GenerateRefreshToken(string userId)
    {
        var refreshToken = await _tokenGenerator.GenerateRefreshToken(userId);
        await _refreshTokenRepository.Add(refreshToken);
        return refreshToken.Token;
    }

    private bool ValidateToken(RefreshToken token)
    {
        return token.ExpiryDate > DateTime.UtcNow;
    }

    public async Task VerifyUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        await _userManager.AddToRoleAsync(user, "Verified");
        user.IsVerificationRequested = false;
        await _context.SaveChangesAsync();
    }

    public async Task DeclineVerification(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        user.IsVerificationRequested = false;
        await _context.SaveChangesAsync();
    }

    public async Task AddVerificationRequest(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        user.IsVerificationRequested = true;
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserDto>> GetVerificationRequests(int? pageNumber, int? pageSize)
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
        var users = await _context.Users
            .Where(u => u.IsVerificationRequested)
            .Skip((pageNumber.Value - 1) * pageSize.Value)
            .Take(pageSize.Value)
            .ToListAsync();
        return _mapper.Map<List<UserDto>>(users);
    }
    public async Task<bool> HasActiveVerificationRequest(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }
        return user.IsVerificationRequested;
    }
}
