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
        private readonly ICloudService _cloudService;
        private readonly IImageRepository _imageRepository;

        public ImageService(ICloudService cloudService, IImageRepository imageRepository)
        {
            _cloudService = cloudService;
            _imageRepository = imageRepository;
        }

        public async Task<(Stream?, string? contentType)> GetImageStreamByIdAsync(string cloudPath, int id)
        {
            var image = await _imageRepository.FindByIdAsync(id);
            if (image == null)
                return (null, null);
            return await GetStream(cloudPath, image);
        }

        public async Task<(Stream?, string? contentType)> GetImageStreamByPathAsync(string cloudPath, string path)
        {
            var image = await _imageRepository.FindImageByPath(path);
            if (image == null)
                return (null, null);
            return await GetStream(path, image);
        }

        public async Task<string?> SaveImage(string cloudPath, Stream imageStream)
        {
            string fileName = Guid.NewGuid().ToString();
            try
            {
                await _cloudService.UploadToR2($"{cloudPath}/{fileName}.png", imageStream);
                await _imageRepository.Add(new Image { Path = fileName, Extension = "png" });
                return fileName;
            }
            catch(Exception ex)
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
