using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Dto;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services;
using OmegaStreamServices.Services.Repositories;
using OmegaStreamServices.Services.VideoServices;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoStreamService : IVideoStreamService
    {
        private readonly string imagesFolder;
        private readonly string videosFolder;

        private readonly ICloudService _cloudServices;
        private readonly IImageRepository _imageRepository;
        private readonly IConfiguration _configuration;

        public VideoStreamService(ICloudService cloudServices, IImageRepository imageRepository, IConfiguration configuration)
        {
            _cloudServices = cloudServices;
            _imageRepository = imageRepository;
            _configuration = configuration;

            videosFolder = _configuration["CloudService:VideoPath"] 
                ?? throw new InvalidOperationException("CloudService:VideoPath configuration is missing.");
            imagesFolder = _configuration["CloudService:ThumbnailPath"]
                ?? throw new InvalidOperationException("CloudService:ThumbnailPath configuration is missing.");
        }

        public async Task<(Stream fileStream, string contentType)> GetFileStreamAsync(string folder, string fileName)
        {
            return await _cloudServices.GetFileStreamAsync($"{folder}/{fileName}");
        }

        public async Task<(Stream imageStream, string contentType)> GetThumbnailStreamAsync(int imageId)
        {
            Image image = await _imageRepository.FindByIdAsync(imageId);
            string fileName = $"{image.Path}.{image.Extension}";
            return await GetFileStreamAsync(imagesFolder, fileName);
        }

        public async Task<(Stream videoStream, string contentType)> GetVideoStreamAsync(string videoKey)
        {
            string folder = videoKey.Split(".").First();
            return await GetFileStreamAsync(videosFolder, $"{folder}/{videoKey}");
        }

        public async Task<(Stream segmentStream, string contentType)> GetVideoSegmentAsync(string segmentKey)
        {
            string folder = GetFolder(segmentKey);
            return await GetFileStreamAsync(videosFolder, $"{folder}/{segmentKey}");
        }

        private string GetFolder(string fileName)
        {
            int suffixLength = 3;
            int extensionIndex = fileName.LastIndexOf('.');
            if (extensionIndex == -1)
                extensionIndex = fileName.Length;

            int folderLength = extensionIndex - suffixLength;
            return fileName.Substring(0, Math.Max(0, folderLength));
        }
    }
}