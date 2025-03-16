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
        public VideoManagementService(IVideoRepository videoRepository, ICloudService cloudService, IVideoLikesRepository videoLikesRepository, IVideoViewRepository videoViewRepository, ICommentRepositroy commentRepository, IImageRepository imageRepository)
        {
            _videoRepository = videoRepository;
            _cloudService = cloudService;
            _videoLikesRepository = videoLikesRepository;
            _videoViewRepository = videoViewRepository;
            _commentRepository = commentRepository;
            _imageRepository = imageRepository;
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

            var deleteFilesTask = _cloudService.DeleteFilesAsync($"videos/{video.Path}");
            var deleteThumbnailTask = _cloudService.DeleteFileAsync($"images/thumbnails/{video.Thumbnail.Path}.{video.Thumbnail.Extension}");

            await Task.WhenAll(deleteFilesTask, deleteThumbnailTask, deleteCommentsTask, deleteReactionsTask, deleteViewsTask);

            await _videoRepository.DeleteVideoWithRelationsAsync(video);
            var image = await _imageRepository.FindByIdAsync(video.ThumbnailId);
            _imageRepository.Delete(image);
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
                var imageToDelete = await _imageRepository.FindByIdAsync(video.ThumbnailId);
                if (imageToDelete != null)
                {
                    await _cloudService.DeleteFileAsync($"images/thumbnails/{imageToDelete.Path}.{imageToDelete.Extension}");
                    _imageRepository.Delete(imageToDelete);
                }

                var newImage = new Image
                {
                    Path = FileManager.GenerateFileName(),
                    Extension = "png"
                };
                await _imageRepository.Add(newImage);
                video.ThumbnailId = newImage.Id;

                await _cloudService.UploadToR2($"images/thumbnails/{newImage.Path}.{newImage.Extension}", image.OpenReadStream());
            }

            _videoRepository.Update(video);
        }

    }
}
