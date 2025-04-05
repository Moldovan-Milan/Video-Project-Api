using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoManagementService : IVideoManagementService
    {
        private readonly IVideoRepository _videoRepository;
        private readonly ICloudService _cloudService;
        private readonly IVideoLikesRepository _videoLikesRepository;
        private readonly IVideoViewRepository _videoViewRepository;
        private readonly ICommentRepositroy _commentRepository;
        private readonly IImageRepository _imageRepository;
        private readonly IImageService _imageService;
        public VideoManagementService(IVideoRepository videoRepository, ICloudService cloudService, IVideoLikesRepository videoLikesRepository, IVideoViewRepository videoViewRepository, ICommentRepositroy commentRepository, IImageRepository imageRepository, IImageService imageService)
        {
            _videoRepository = videoRepository;
            _cloudService = cloudService;
            _videoLikesRepository = videoLikesRepository;
            _videoViewRepository = videoViewRepository;
            _commentRepository = commentRepository;
            _imageRepository = imageRepository;
            _imageService = imageService;
        }
        public async Task DeleteVideoWithAllRelations(int id)
        {
            Video video = await _videoRepository.GetVideoWithInclude(id);
            if (video == null)
            {
                throw new Exception($"Video with ID {id} not found.");
            }

            var commentsTask = _commentRepository.GetAllCommentsByVideo(video.Id);
            var reactionsTask = _videoLikesRepository.GetAllReactionsByVideo(video.Id);
            var viewsTask = _videoViewRepository.GetAllVideoViewsByVideo(video.Id);

            await Task.WhenAll(commentsTask, reactionsTask, viewsTask);

            var deleteCommentsTask = _commentRepository.DeleteMultipleAsync(commentsTask.Result);
            var deleteReactionsTask = _videoLikesRepository.DeleteMultipleAsync(reactionsTask.Result);
            var deleteViewsTask = _videoViewRepository.DeleteMultipleAsync(viewsTask.Result);

            try
            {
                await _cloudService.DeleteFilesAsync($"videos/{video.Path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to delete video file {video.Path}. Error: {ex.Message}");
            }

            try
            {
                await _cloudService.DeleteFileAsync($"images/thumbnails/{video.Thumbnail.Path}.{video.Thumbnail.Extension}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to delete video thumbnail {video.Thumbnail.Path}. Error: {ex.Message}");
            }

            await Task.WhenAll(deleteCommentsTask, deleteReactionsTask, deleteViewsTask);

            await _videoRepository.DeleteVideoWithRelationsAsync(video);

            var image = await _imageRepository.FindByIdAsync(video.ThumbnailId);
            if (image != null)
            {
                _imageRepository.Delete(image);
            }
        }


        public async Task EditVideo(int id, string? title, string? description, IFormFile? image)
        {
            Video video = await _videoRepository.GetVideoWithInclude(id);
            if (video == null)
            {
                throw new KeyNotFoundException($"Video with ID {id} not found.");
            }

            if (title != null)
            {
                video.Title = title;
            }
            if (description != null)
            {
                video.Description = description;
            }
            if (image != null)
            {
                await _imageService.ReplaceImage("images/thumbnails", video.Thumbnail.Path, image.OpenReadStream());
                
            }
            _videoRepository.Update(video);
        }

    }
}
