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
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IAvatarService _avatarService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;
    private readonly IImageRepository _imageRepository;
    private readonly IVideoManagementService _videoManagementService;
    private readonly ICommentRepositroy _commentRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IVideoLikesRepository _videoLikesRepository;
    private readonly IVideoViewRepository _videoViewRepository;
    private readonly IUserChatsRepository _userChatsRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ICloudService _cloudService;

    private readonly byte[] JWT_KEY;
    private readonly string ISSUER;

    public UserService(UserManager<User> userManager, IPasswordHasher<User> passwordHasher,
        SignInManager<User> signInManager, IConfiguration configuration,
        IAvatarService avatarService, IRefreshTokenService refreshTokenService,
        IMapper mapper,
        AppDbContext context,
        IImageRepository imageRepository,
        IVideoManagementService videoManagementService,
        ICommentRepositroy commentRepository, ISubscriptionRepository subscriptionRepository,
        IVideoLikesRepository videoLikesRepository,
        IVideoViewRepository videoViewRepository,
        IUserChatsRepository userChatsRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ICloudService cloudService)
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
        _context = context;

        _imageRepository = imageRepository;
        _videoManagementService = videoManagementService;
        _commentRepository = commentRepository;
        _subscriptionRepository = subscriptionRepository;
        _videoLikesRepository = videoLikesRepository;
        _videoViewRepository = videoViewRepository;
        _userChatsRepository = userChatsRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _cloudService = cloudService;
    }

    public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
    {
        string avatarFileName = await _avatarService.SaveAvatarAsync(avatar);
        var avatarImage = await _imageRepository.FindImageByPath(avatarFileName);
        var user = new User
        {
            UserName = username,
            Email = email,
            AvatarId = avatarImage != null ? avatarImage.Id : 0,
            Created = DateTime.UtcNow,
        };

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

    public async Task<bool> UpdateUsername(User user, string newName)
    {
        var result = await _userManager.SetUserNameAsync(user, newName);
        await _userManager.UpdateNormalizedUserNameAsync(user);
        _context.SaveChangesAsync();
        return result.Succeeded;
    }

    public async Task DeleteAccount(string userId)
    {

        using (var transaction = await _context.Database.BeginTransactionAsync())
        {
            var user = await _userManager.Users.Include(x => x.Avatar).FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null) return;

            var videos = _context.Videos.Where(x => x.UserId == userId);
            foreach (var video in videos)
            {
                await _videoManagementService.DeleteVideoWithAllRelations(video.Id);
            }

            _context.Comments.RemoveRange(_context.Comments.Where(x => x.UserId == userId));
            _context.Subscriptions.RemoveRange(_context.Subscriptions.Where(x => x.FollowerId == userId || x.FollowedUserId == userId));
            _context.VideoLikes.RemoveRange(_context.VideoLikes.Where(x => x.UserId == userId));
            _context.VideoViews.RemoveRange(_context.VideoViews.Where(x => x.UserId == userId));

            var chatIds = _context.UserChats.Where(x => x.User1Id == userId || x.User2Id == userId)
                .Select(x => x.Id).ToList();
            _context.ChatMessages.RemoveRange(_context.ChatMessages.Where(x => chatIds.Contains(x.UserChatId)));
            _context.UserChats.RemoveRange(_context.UserChats.Where(x => x.User1Id == userId || x.User2Id == userId));

            _context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(x => x.UserId == userId));

            if (user.Avatar != null && user.Avatar.Path != "default_avatar")
            {
                var avatar = await _imageRepository.FindByIdAsync(user.AvatarId);
                await _cloudService.DeleteFileAsync($"images/avatars/{user.Avatar.Path}.{user.Avatar.Extension}");
                _imageRepository.Delete(avatar);
            }

            // TODO: Delete Banner

            await _userManager.DeleteAsync(user);
            await transaction.CommitAsync();
        }

    }


}
