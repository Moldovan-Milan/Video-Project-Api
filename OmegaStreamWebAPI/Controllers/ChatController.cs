using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IGenericRepository _repo;
        private readonly IMapper _mapper;
        private readonly IEncryptionHelper _encryptionHelper;

        private readonly UserManager<User> _userManager;


        public ChatController(IMapper mapper, UserManager<User> userManager, IGenericRepository repo, IEncryptionHelper encryptionHelper)
        {
            _mapper = mapper;
            _userManager = userManager;
            _repo = repo;
            _encryptionHelper = encryptionHelper;
        }

        [HttpGet("user-chats")]
        public async Task<IActionResult> GetUserChats()
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized();
            }

            //var userChats = await _userChatsRepository.GetAllChatByUserIdAsync(userIdFromToken);
            var userChats = await _repo.GetAllAsync<UserChats>
                (
                    filter: chat => chat.User1Id == userIdFromToken || chat.User2Id == userIdFromToken,
                    include: chat => chat.Include(x => x.User1).Include(x => x.User2)
                );

            if (userChats != null)
            {
                var userChatsDtoList = new List<UserChatsDto>();

                foreach (var chat in userChats)
                {
                    var user = chat.User1.Id == userIdFromToken ? chat.User2 : chat.User1;
                    var messages = await _repo.GetAllAsync<ChatMessage>(m => m.UserChatId == chat.Id);
                    var lastMessage = messages.OrderByDescending(m => m.SentAt)
                        .Select(m => m.Content)
                        .FirstOrDefault();

                    var userChatsDto = new UserChatsDto
                    {
                        Id = chat.Id,
                        User = _mapper.Map<UserDto>(user),
                        LastMessage = lastMessage != null ? _encryptionHelper.Decrypt(lastMessage)
                            : string.Empty
                    };

                    userChatsDtoList.Add(userChatsDto);
                }

                return Ok(userChatsDtoList);
            }
            return NoContent();
        }


        [HttpPost]
        [Route("new-chat")]
        public async Task<IActionResult> CreateChat([FromForm] string userId)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized();
            }

            bool isExist = await _repo.AnyAsync<UserChats>(
                chat => chat.User1Id == userIdFromToken && chat.User2Id == userId
                || chat.User2Id == userIdFromToken && chat.User1Id == userId);

            if (isExist)
            {
                var chat = await _repo.FirstOrDefaultAsync<UserChats>(chat => chat.User1Id == userIdFromToken && chat.User2Id == userId
                    || chat.User2Id == userIdFromToken && chat.User1Id == userId);
                if (chat != null)
                    return Ok(chat.Id);
            }

            User? user = await _userManager.FindByIdAsync(userIdFromToken);
            User? user2 = await _userManager.FindByIdAsync(userId);
            if (user == null || user2 == null)
                return NotFound(new { Message = "User not exist" });

            UserChats userChat = new UserChats
            {
                Created = DateTime.UtcNow,
                User1 = user,
                User2 = user2,
            };
            await _repo.AddAsync(userChat);

            return Ok(userChat.Id);
        }
    }
}
