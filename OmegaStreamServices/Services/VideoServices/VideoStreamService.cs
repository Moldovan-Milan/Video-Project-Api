using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Dto;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoStreamService : IVideoStreamService
    {
        // Constans
        private const string ThumbnailPath = "thumbnails";
        private const string ImagesFolder = "images";
        private const string VideosFolder = "videos";

        // Repository
        private readonly IVideoRepository _videoRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IVideoLikesRepository _videoLikesRepository;
        private readonly ICommentRepositroy _commentRepositroy;

        private readonly UserManager<User> _userManager;

        // R2
        private readonly ICloudService _cloudServices;

        // Mapper
        private readonly IMapper _mapper;

        public VideoStreamService(IConfiguration configuration, IVideoRepository videoRepository,
            IImageRepository imageRepository, ICloudService cloudServices,
            IVideoLikesRepository videoLikesRepository, IMapper mapper, UserManager<User> userManager, 
            ICommentRepositroy commentRepositroy)
        {
            // Repos
            _videoRepository = videoRepository;
            _imageRepository = imageRepository;
            _videoLikesRepository = videoLikesRepository;

            _userManager = userManager;

            // R2
            _cloudServices = cloudServices;

            // Mapper
            _mapper = mapper;
            _commentRepositroy = commentRepositroy;
        }

        #region Stream
        private async Task<(Stream fileStream, string contentType)> GetFileStreamAsync(string folder, string fileName)
        {
            return await _cloudServices.GetFileStreamAsync($"{folder}/{fileName}");
        }


        public async Task<(Stream imageStream, string contentType)> GetStreamAsync(int imageId, string path = ThumbnailPath)
        {
            Image image = await _imageRepository.FindByIdAsync(imageId);
            string fileName = $"{path}/{image.Path}.{image.Extension}";
            return await GetFileStreamAsync(ImagesFolder, fileName);
        }

        public async Task<(Stream segmentStream, string contentType)> GetVideoSegmentAsync(string segmentKey)
        {
            string folder = GetFolder(segmentKey);
            return await GetFileStreamAsync(VideosFolder, $"{folder}/{segmentKey}");
        }

        public async Task<(Stream videoStream, string contentType)> GetVideoStreamAsync(string videoKey)
        {
            string folder = videoKey.Split(".").First();
            return await GetFileStreamAsync(VideosFolder, $"{folder}/{videoKey}");
        }

        private string GetFolder(string fileName)
        {
            // Vegyük az utolsó három karakter előtti részt, a kiterjesztés figyelembevételével
            int suffixLength = 3; // Az utolsó három számjegy
            int extensionIndex = fileName.LastIndexOf('.');
            if (extensionIndex == -1)
                extensionIndex = fileName.Length;

            int folderLength = extensionIndex - suffixLength;
            return fileName.Substring(0, Math.Max(0, folderLength));
        }

        #endregion Stream


        #region MetaData
        public async Task<List<Video>> GetAllVideosMetaData()
        {
            return await _videoRepository.GetAllVideosWithIncludes();
        }

        public async Task<VideoDto> GetVideoMetaData(int id)
        {
            Video video = await _videoRepository.GetVideoWithInclude(id);

            UserDto userDto = _mapper.Map<UserDto>(video.User);
            int likes = await _videoLikesRepository.GetLikesByVideoId(video.Id);
            int dislikes = await _videoLikesRepository.GetDisLikesByVideoId(video.Id);

            VideoDto videoDto = _mapper.Map<VideoDto>(video);
            videoDto.Likes = likes;
            videoDto.Dislikes = dislikes;
            videoDto.User = userDto;

            return videoDto;
        }

        public async Task<string> IsUserLikedVideo(string userId, int videoId)
        {
            return await _videoLikesRepository.IsLikedByUser(userId, videoId);
        }

        public async Task<bool> UpdateUserLikedVideo(int videoId, string userId, string likeValue)
        {
            try
            {
                bool isLikeExist = true;
                Video video = await _videoRepository.FindByIdAsync(videoId);
                User user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null || video == null)
                {
                    return false;
                }

                VideoLikes videoLikes = await _videoLikesRepository.GetVideoLike(userId, videoId);
                if (videoLikes != null && likeValue == "none")
                {
                    _videoLikesRepository.Delete(videoLikes);
                    return true;
                }
                if (videoLikes == null && likeValue == "none")
                {
                    return false;
                }
                if (videoLikes == null && likeValue != "none")
                {
                    isLikeExist = false;
                    videoLikes = new VideoLikes
                    {
                        UserId = userId,
                        VideoId = videoId,
                    };
                }

                switch (likeValue)
                {
                    case "like":
                        videoLikes.IsDislike = false;
                        break;
                    case "dislike":
                        videoLikes.IsDislike = true;
                        break;
                }
                // Ha nincs felvéve, akkor létre kell hozni, hogy ne fusson le hibával
                if (!isLikeExist) 
                {
                    await _videoLikesRepository.Add(videoLikes);
                }
                else
                {
                    _videoLikesRepository.Update(videoLikes);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Video>> GetVideosByName(string name)
        {
            return await _videoRepository.GetVideosByName(name); 
        }

        public async Task<int> AddNewComment(NewCommentDto newComment, string UserId)
        {
            User user = await _userManager.FindByIdAsync(UserId)!;
            Video video = await _videoRepository.FindByIdAsync(newComment.VideoId);
            if (user == null || video == null)
            {
                return -1;
            }
            Comment comment = new Comment
            {
                Content = newComment.Content,
                UserId = user.Id,
                Created = DateTime.Now,
                VideoId = newComment.VideoId,
                User = user,
                Video = video
            };
            await _commentRepositroy.Add(comment);
            return comment.Id;
        }



        #endregion
    }
}
