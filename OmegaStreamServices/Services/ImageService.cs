using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public class ImageService : IImageService
    {
        private readonly IGenericRepository _repo;
        private readonly ICloudService _cloudService;

        public ImageService(ICloudService cloudService, IGenericRepository repo)
        {
            _cloudService = cloudService;
            _repo = repo;
        }

        public async Task<(Stream?, string? contentType)> GetImageStreamByIdAsync(string cloudPath, int id)
        {
            var image = await _repo.FirstOrDefaultAsync<Image>(i => i.Id == id);
            if (image == null)
                return (null, null);
            return await GetStream(cloudPath, image);
        }

        public async Task<(Stream?, string? contentType)> GetImageStreamByPathAsync(string cloudPath, string path)
        {
            var image = await _repo.FirstOrDefaultAsync<Image>(i => i.Path == path);
            if (image == null)
                return (null, null);
            return await GetStream(path, image);
        }

        public async Task<bool> ReplaceImage(string cloudPath, string imagePath, Stream image)
        {
            string? result = await SaveImage(cloudPath, imagePath, image);
            return result == null ? false : true;
        }

        public async Task<string?> SaveImage(string cloudPath, Stream imageStream)
        {
            string fileName = Guid.NewGuid().ToString();
            await _repo.AddAsync(new Image { Path = fileName, Extension = "png" });
            return await SaveImage(cloudPath, fileName, imageStream);
        }

        private async Task<string?> SaveImage(string cloudPath, string fileName, Stream imageStream)
        {
            try
            {
                await _cloudService.UploadToR2($"{cloudPath}/{fileName}.png", imageStream);
                return fileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error happened: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<(Stream? stream, string? contentType)> GetStream(string path, Image image)
        {
            try
            {
                return await _cloudService.GetFileStreamAsync($"{path}/{image.Path}.{image.Extension}");
            }
            catch
            {
                return (null, null);
            }
        }
    }
}
