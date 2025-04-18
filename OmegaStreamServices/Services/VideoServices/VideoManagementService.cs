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
        private readonly IGenericRepository _repo;

        private readonly ICloudService _cloudService;
        private readonly IImageService _imageService;
        public VideoManagementService(ICloudService cloudService, IImageService imageService, IGenericRepository repo)
        {
            _imageService = imageService;
            _cloudService = cloudService;
            _repo = repo;
        }
        public async Task DeleteVideoWithAllRelations(int id)
        {
            Video? video = await _repo.FirstOrDefaultAsync<Video>(
                predicate: x => x.Id == id
                );

            if (video == null)
            {
                throw new Exception($"Video with ID {id} not found.");
            }

            var comments = await _repo.GetAllAsync<Comment>(x => x.VideoId == video.Id);
            var reactions = await _repo.GetAllAsync<VideoLikes>(x => x.VideoId == video.Id);
            var views = await _repo.GetAllAsync<VideoView>(x => x.VideoId == video.Id);

            await _repo.DeleteMultipleAsync(comments);
            await _repo.DeleteMultipleAsync(reactions);
            await _repo.DeleteMultipleAsync(views);

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

            await _repo.DeleteAsync(video);

            //var image = await _imageRepository.FindByIdAsync(video.ThumbnailId);
            var image = await _repo.FirstOrDefaultAsync<Image>(predicate: x => x.Id == video.ThumbnailId);
            if (image != null)
            {
                await _repo.DeleteAsync(image);
            }
        }


        public async Task EditVideo(int id, string? title, string? description, IFormFile? image)
        {
            //Video video = await _videoRepository.GetVideoWithInclude(id);
            Video? video = await _repo.FirstOrDefaultAsync<Video>(
                predicate: x => x.Id == id,
                include: x => x.Include(x => x.Thumbnail));
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
            await _repo.UpdateAsync(video);
        }

    }
}
