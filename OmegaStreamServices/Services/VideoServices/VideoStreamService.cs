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

        // R2
        private readonly ICloudService _cloudServices;

        public VideoStreamService(IConfiguration configuration, IVideoRepository videoRepository,
            IImageRepository imageRepository, ICloudService cloudServices)
        {
            // Db conn
            _videoRepository = videoRepository;
            _imageRepository = imageRepository;

            // R2
            _cloudServices = cloudServices;
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
            return await _videoRepository.GetAll();
        }

        public async Task<Video> GetVideoMetaData(int id)
        {
            return await _videoRepository.GetVideoWithInclude(id); ;
        }
        #endregion
    }
}
