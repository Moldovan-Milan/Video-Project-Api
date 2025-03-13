using AutoMapper;
using Microsoft.AspNetCore.Authorization;
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
    public class LiveStreamController : ControllerBase
    {
        private readonly ILiveStreamRepository _liveStreamRepository;
        private readonly IMapper _mapper;

        public LiveStreamController(ILiveStreamRepository liveStreamRepository, IMapper mapper)
        {
            _liveStreamRepository = liveStreamRepository;
            _mapper = mapper;
        }

        [HttpPost("start")]
        [Authorize]
        public async Task<IActionResult> StartLiveStream([FromQuery] string streamTitle, [FromQuery] string? description)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                return Unauthorized();
            }



            var liveStreamId = Guid.NewGuid().ToString();
            var liveStream = new LiveStream
            {
                Id = liveStreamId,
                UserId = userIdFromToken,
                StartedAt = DateTime.UtcNow,
                StreamTitle = streamTitle,
                Description = description == null ? string.Empty : description
            };

            await _liveStreamRepository.AddLiveStreamAsync(liveStream);

            return Ok(new { StreamId = liveStreamId });
        }

        [HttpGet("{streamId}")]
        public async Task<IActionResult> GetLiveStream(string streamId)
        {
            var liveStream = await _liveStreamRepository.GetLiveStreamByIdAsync(streamId);
            if (liveStream == null)
            {
                return NotFound();
            }

            var liveStreamDto = _mapper.Map<LiveStreamDto>(liveStream);
            return Ok(liveStreamDto);
        }

        [HttpPost("stop/{streamId}")]
        public async Task<IActionResult> StopLiveStream(string streamId)
        {
            var liveStream = await _liveStreamRepository.GetLiveStreamByIdAsync(streamId);
            if (liveStream == null)
            {
                return NotFound();
            }

            liveStream.EndedAt = DateTime.UtcNow;
            await _liveStreamRepository.UpdateLiveStreamAsync(liveStream);

            return Ok();
        }
    }
}
