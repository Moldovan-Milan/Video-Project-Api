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

        public VideoMetadataService(IVideoRepository videoRepository, IMapper mapper, IVideoLikesRepository videoLikeService)
        {
            _videoRepository = videoRepository;
            _mapper = mapper;
            _videoLikeRepository = videoLikeService;
        }

        public async Task<List<VideoDto>> GetAllVideosMetaData()
        {
            var videos = await _videoRepository.GetAllVideosWithIncludes();
            return ConvertVideoListToDto(videos);
        }

        public async Task<VideoDto> GetVideoMetaData(int id)
        {
            Video video = await _videoRepository.GetVideoWithInclude(id);
            VideoDto videoDto = _mapper.Map<VideoDto>(video);
            videoDto.Likes = await _videoLikeRepository.GetLikesByVideoId(video.Id);
            videoDto.Dislikes = await _videoLikeRepository.GetDisLikesByVideoId(video.Id);
            return videoDto;
        }

        public async Task<List<VideoDto>> GetVideosByName(string name)
        {
            var videos = await _videoRepository.GetVideosByName(name);
            return ConvertVideoListToDto(videos);
        }

        private List<VideoDto> ConvertVideoListToDto(List<Video> videos)
        {
            List<VideoDto> result = new();
            foreach (var video in videos)
            {
                result.Add(_mapper.Map<VideoDto>(video));
            }
            return result;
        }
    }
}