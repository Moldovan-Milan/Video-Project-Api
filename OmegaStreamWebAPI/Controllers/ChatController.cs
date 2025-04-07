using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IUserChatsRepository _userChatsRepository;
        private readonly IMapper _mapper;
        private readonly IChatMessageRepository _chatMessageRepository;
        private readonly UserManager<User> _userManager;


        public ChatController(IUserChatsRepository userChatsRepository, IMapper mapper, IChatMessageRepository chatMessageRepository, UserManager<User> userManager)
        {
            _userChatsRepository = userChatsRepository;
            _mapper = mapper;
            _chatMessageRepository = chatMessageRepository;
            _userManager = userManager;
        }

        [HttpGet("user-chats")]
        public async Task<IActionResult> GetUserChats()
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized();
            }

            var userChats = await _userChatsRepository.GetAllChatByUserIdAsync(userIdFromToken);
            if (userChats != null)
            {
                var userChatsDtoList = new List<UserChatsDto>();

                foreach (var chat in userChats)
                {
                    var user = chat.User1.Id == userIdFromToken ? chat.User2 : chat.User1;
                    var lastMessage = await _chatMessageRepository.GetLastMessageByChatId(chat.Id);

                    var userChatsDto = new UserChatsDto
                    {
                        Id = chat.Id,
                        User = _mapper.Map<UserDto>(user),
                        LastMessage = lastMessage
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

            var (isExist, chat) = await _userChatsRepository.HasUserChat(userIdFromToken, userId);
            if (isExist)
                return Ok(chat.Id);

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
            await _userChatsRepository.Add(userChat);

            return Ok(userChat.Id);
        }
    }
}
