using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Dto;
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

        public ChatController(IUserChatsRepository userChatsRepository, IMapper mapper, IChatMessageRepository chatMessageRepository)
        {
            _userChatsRepository = userChatsRepository;
            _mapper = mapper;
            _chatMessageRepository = chatMessageRepository;
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
            var userChatsDto = await Task.WhenAll(userChats.Select(async chat => new UserChatsDto
            {
                Id = chat.Id,
                User = _mapper.Map<UserDto>(chat.User1.Id == userIdFromToken ? chat.User2 : chat.User1),
                LastMessage = await _chatMessageRepository.GetLastMessageByChatId(chat.Id)
            }));

            return Ok(userChatsDto);
        }
    }
}
