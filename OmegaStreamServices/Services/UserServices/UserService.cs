﻿using AutoMapper;
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
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IAvatarService _avatarService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IMapper _mapper;
    private readonly AppDbContext _context;
    private readonly IImageRepository _imageRepository;
    private readonly IUserThemeRepository _userThemeRepository;
    private readonly ICloudService _cloudService;

    private readonly byte[] JWT_KEY;
    private readonly string ISSUER;

    public UserService(UserManager<User> userManager, IPasswordHasher<User> passwordHasher,
        SignInManager<User> signInManager, IConfiguration configuration,
        IAvatarService avatarService, IRefreshTokenService refreshTokenService, IMapper mapper, AppDbContext context, IImageRepository imageRepository, IUserThemeRepository userThemeRepository, ICloudService cloudService)
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
        _userThemeRepository = userThemeRepository;
        _cloudService = cloudService;
    }

    public async Task<IdentityResult> RegisterUser(string username, string email, string password, Stream avatar)
    {
        //if (await _userManager.FindByEmailAsync(email) != null)
        //{
        //    return IdentityResult.Failed(new IdentityError { Description = "Email already exists." });
        //}

        string avatarFileName = await _avatarService.SaveAvatarAsync(avatar);
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
        _context.SaveChangesAsync();
        return result.Succeeded;
    }

    public async Task<bool> SaveTheme(string background, string textColor, Stream bannerImage, User user)
    {
        try
        {
            UserTheme userTheme = new UserTheme
            {
                Background = background,
                TextColor = textColor
            };

            string fileName = await SaveBanner(bannerImage);
            var image = await _imageRepository.FindImageByPath(fileName);
            userTheme.BannerId = image.Id;
            await _userThemeRepository.Add(userTheme);

            user.UserThemeId = userTheme.Id;
            await _userManager.UpdateAsync(user);
            return true;
        }
       catch(Exception ex)
       {
            Console.WriteLine(ex);
            return false;
       }
        
    }

    private async Task<string> SaveBanner(Stream avatarStream)
    {
        string fileName = Guid.NewGuid().ToString();
        string imagePath = $"images/banner/{fileName}.png";

        await _cloudService.UploadToR2(imagePath, avatarStream);
        await _imageRepository.Add(new Image { Path = fileName, Extension = "png" });

        return fileName;
    }

    public async Task<(Stream file, string contentType)> GetBannerAsync(int bannerId)
    {
        var image = await _imageRepository.FindByIdAsync(bannerId);
        if (image == null)
        {
            throw new Exception("Banner not found.");
        }
        return await _cloudService.GetFileStreamAsync($"images/banner/{image.Path}.{image.Extension}");
    }

    /*
     User user = await _userManager.Users.Include(x => x.UserTheme).FirstOrDefaultAsync(x => x.Id == userId)!;
        if (user.UserTheme == null)
        {
            user.UserTheme = new();
        }

        if (user.UserTheme.TextColor !=  textColor)
        {
            user.UserTheme.TextColor = textColor;
        }
        if (user.UserTheme.Background != background)
        {
            user.UserTheme.Background = background;
        }
     */
}
