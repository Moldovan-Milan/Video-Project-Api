using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        public ChatController(IUserChatsRepository userChatsRepository, IMapper mapper)
        {
            _userChatsRepository = userChatsRepository;
            _mapper = mapper;
        }

        [Route("user-chats")]
        [HttpGet]
        public async Task<IActionResult> GetUserChats()
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return BadRequest();
            }
            var userChats = await _userChatsRepository.GetAllChatByUserIdAsync(userIdFromToken);
            var userChatsDto = _mapper.Map<List<UserChatsDto>>(userChats);

            return Ok(userChatsDto);
        }
    }
}
