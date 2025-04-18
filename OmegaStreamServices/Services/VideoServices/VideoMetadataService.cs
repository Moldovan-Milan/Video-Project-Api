using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoMetadataService : IVideoMetadataService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly IMapper _mapper;
        private readonly IGenericRepository _repo;

        public VideoMetadataService(IVideoRepository videoRepository, IMapper mapper, IGenericRepository repo)
        {
            _videoRepository = videoRepository;
            _mapper = mapper;
            _repo = repo;
        }

        public async Task<List<VideoDto?>> GetAllVideosMetaData(int? pageNumber, int? pageSize, bool isShorts)
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
            
            var videos = await _videoRepository.GetAllVideosWithIncludes(pageNumber.Value, pageSize.Value, isShorts);
            return _mapper.Map<List<VideoDto?>>(videos);
        }

        public async Task<VideoDto?> GetVideoMetaData(int id)
        {
            try
            {
                Video? video = await _repo.FirstOrDefaultAsync<Video>(
                    predicate: x => x.Id == id,
                    include: x => x.Include(x => x.User)
                                .ThenInclude(x => x.Avatar)
                                .Include(x => x.Comments)
                                .ThenInclude(x => x.User)
                                .ThenInclude(x => x.Avatar)
                                .Include(x => x.VideoLikes));

                if (video == null)
                {
                    return null;
                }

                VideoDto videoDto = _mapper.Map<VideoDto>(video);
                videoDto.User.FollowersCount = await _repo.CountAsync<Subscription>(
                    predicate: x => x.FollowedUserId == videoDto.UserId);

                videoDto.Likes = await _repo.CountAsync<VideoLikes>(
                    predicate: x => x.VideoId == video.Id && x.IsDislike == false
                );

                videoDto.Dislikes = await _repo.CountAsync<VideoLikes>(
                    predicate: x => x.VideoId == video.Id && x.IsDislike == true
                );

                return videoDto;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<List<VideoDto>?> GetVideosByName(string name, int? pageNumber, int? pageSize, bool isShorts)
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
            var videos = await _videoRepository.GetVideosByName(name, pageNumber.Value, pageSize.Value, isShorts);
            if (videos == null || videos.Count == 0)
            {
                return null;
            }
            return _mapper.Map<List<VideoDto>>(videos);
        }
    }
}