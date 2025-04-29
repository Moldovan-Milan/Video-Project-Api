using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System.Security.Claims;

namespace OmegaStreamWebAPI.Controllers
{
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

        [HttpGet]
        public async Task<IActionResult> GetAllStream()
        {
            var liveStreams = await _liveStreamRepository.GetAllLiveStreamsAsync();
            if (liveStreams == null)
                return NotFound();
            return Ok(_mapper.Map <List<LiveStreamDto>>(liveStreams));
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

    }
}
