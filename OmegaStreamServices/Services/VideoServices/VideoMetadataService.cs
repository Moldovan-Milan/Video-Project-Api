﻿using AutoMapper;
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
        private readonly IVideoLikesRepository _videoLikeRepository;
        private readonly IMapper _mapper;
        private readonly ISubscriptionRepository _subscriptionRepository;

        public VideoMetadataService(IVideoRepository videoRepository, IMapper mapper, IVideoLikesRepository videoLikeService, ISubscriptionRepository subscriptionRepository)
        {
            _videoRepository = videoRepository;
            _mapper = mapper;
            _videoLikeRepository = videoLikeService;
            _subscriptionRepository = subscriptionRepository;
        }

        public async Task<List<VideoDto?>> GetAllVideosMetaData()
        {
            var videos = await _videoRepository.GetAllVideosWithIncludes();
            return _mapper.Map<List<VideoDto?>>(videos);
        }

        public async Task<VideoDto?> GetVideoMetaData(int id)
        {
            Video video = await _videoRepository.GetVideoWithInclude(id);
            VideoDto videoDto = _mapper.Map<VideoDto>(video);
            videoDto.User.FollowersCount = await _subscriptionRepository.GetFollowersCount(videoDto.UserId);
            videoDto.Likes = await _videoLikeRepository.GetLikesByVideoId(video.Id);
            videoDto.Dislikes = await _videoLikeRepository.GetDisLikesByVideoId(video.Id);
            return videoDto;
        }
        public async Task<List<VideoDto?>> GetVideosByName(string name)
        {
            var videos = await _videoRepository.GetVideosByName(name);
            return _mapper.Map<List<VideoDto?>>(videos);
        }
    }
}