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
    private readonly IGenericRepository _repo;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;
    private readonly IImageService _imageService;
    private readonly IVideoManagementService _videoManagementService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICloudService _cloudService;
    private readonly string avatarPath;
    private readonly string bannerPath;
    private readonly IConfiguration _config;


    public UserService(UserManager<User> userManager,
        SignInManager<User> signInManager,
        IMapper mapper,
        AppDbContext context,
         ICloudService cloudService, IImageService imageService,
        IVideoManagementService videoManagementService, IRefreshTokenRepository refreshTokenRepository, IGenericRepository repo,
        IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _mapper = mapper;

        _context = context;
        _cloudService = cloudService;
        _imageService = imageService;
        _videoManagementService = videoManagementService;

        _repo = repo;
        _refreshTokenRepository = refreshTokenRepository;
        _config = config;
        avatarPath = _config["CloudService:AvatarPath"];
        bannerPath = _config["CloudService:BannerPath"];
    }

    public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
    {

        if (await _userManager.Users.AnyAsync(x => x.Email == email))
            return IdentityResult.Failed(new IdentityError
            {
                Code = "Duplicate",
                Description = "Email already exist."
            });

        string avatarFileName = await _imageService.SaveImage("images/avatars", avatar);
        //var avatarImage = await _imageRepository.FindImageByPath(avatarFileName);
        var avatarImage = await _repo.FirstOrDefaultAsync<Image>(x => x.Path == avatarFileName);
        var user = new User
        {
            UserName = username,
            Email = email,
            AvatarId = avatarImage != null ? avatarImage.Id : 0,
            Created = DateTime.UtcNow,
        };

        var result = await _userManager.CreateAsync(user, password);

        return result;

    }

    public async Task<(string? refreshToken, User)> LoginUser(string email, string password, bool rememberMe)
    {
        var user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Email == email);
        if (user == null)
            return (null, null)!;

        var result = await _signInManager.PasswordSignInAsync(user.UserName, password, rememberMe, true);
        if (!result.Succeeded) return (null, null)!;

        string? refreshToken = rememberMe ? await GenerateRefreshToken(user.Id) : null;
        return (refreshToken, user);
    }

    public async Task LogoutUser()
    {
        await _signInManager.SignOutAsync();
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
            .Include(x => x.Avatar)
            .Include(x => x.Videos)
            .ThenInclude(v => v.Thumbnail)
            .Include(x => x.Followers)
            .Include(x => x.UserTheme)
            .ThenInclude(x => x.BannerImg)
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
            .Include(x => x.Avatar)
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
                    var existingImage = await _repo.FirstOrDefaultAsync<Image>(x => x.Id == userTheme.BannerId.Value);
                    if (existingImage != null)
                    {
                        if (!await _imageService.ReplaceImage("images/banners", existingImage.Path, bannerImage))
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

                    var image = await _repo.FirstOrDefaultAsync<Image>(x => x.Path == fileName);
                    if (image != null)
                    {
                        userTheme.BannerId = image.Id;
                    }
                }
            }
            await _repo.UpdateAsync(userTheme);
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
        return await _imageService.SaveImage(bannerPath, bannerStream);
    }

    public async Task DeleteAccount(string userId)
    {
        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            try
            {
                var user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null) return;

                //var videos = _context.Videos.Where(x => x.UserId == userId);
                var videos = await _repo.GetAllAsync<Video>(x => x.UserId == userId);
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

                await _repo.DeleteMultipleAsync<Comment>(x => x.UserId == userId);
                await _repo.DeleteMultipleAsync<Subscription>(x => x.FollowerId == userId || x.FollowedUserId == userId);
                await _repo.DeleteMultipleAsync<VideoLikes>(x => x.UserId == userId);
                await _repo.DeleteMultipleAsync<VideoView>(x => x.UserId == userId);

                var chats = await _repo.GetAllAsync<UserChats>(x => x.User1Id == userId || x.User2Id == userId);
                await _repo.DeleteMultipleAsync<ChatMessage>(x => chats.Select(c => c.Id).Contains(x.UserChatId));
                await _repo.DeleteMultipleAsync<UserChats>(x => x.User1Id == userId || x.User2Id == userId);


                if (user.Avatar != null && user.Avatar.Path != "default_avatar")
                {
                    try
                    {
                        var avatar = await _repo.FirstOrDefaultAsync<Image>(x => x.Id == user.AvatarId);
                        await _cloudService.DeleteFileAsync($"images/avatars/{user.Avatar.Path}.{user.Avatar.Extension}");
                        await _repo.DeleteAsync<Image>(avatar);
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

        var newRefreshToken = await TokenGenerator.GenerateRefreshToken(user.Id);
        _refreshTokenRepository.Delete(refreshTokenObj);
        await _refreshTokenRepository.Add(newRefreshToken);

        await _signInManager.SignInAsync(user, true);

        return (newRefreshToken.Token, user);
    }

    private async Task<string> GenerateRefreshToken(string userId)
    {
        var refreshToken = await TokenGenerator.GenerateRefreshToken(userId);
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
            .Include(u => u.Avatar)
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

    public async Task<bool> ChangeAvatar(Stream avatar, string userId)
    {
        User? user = await _userManager.Users.Include(x => x.Avatar)
            .FirstOrDefaultAsync(x => x.Id == userId);
        if (user == null)
        {
            return false;
        }
        if (await _imageService.ReplaceImage(avatarPath, user.Avatar.Path, avatar))
        {
            return true;
        }
        return false;
    }
}
