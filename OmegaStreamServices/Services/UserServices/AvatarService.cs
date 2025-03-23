using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Services.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.UserServices
{
    public class AvatarService: IAvatarService
    {
        private readonly ICloudService _cloudService;
        private readonly IImageRepository _imageRepository;
        private readonly IConfiguration _configuration;

        private readonly string avatarPath;
        private readonly string defaultAvatarFormat;

        public AvatarService(ICloudService cloudService, IImageRepository imageRepository, IConfiguration configuration)
        {
            _cloudService = cloudService;
            _imageRepository = imageRepository;
            _configuration = configuration;

            avatarPath = _configuration["CloudService:AvatarPath"]
                ?? throw new InvalidOperationException("CloudService:AvatarPathPath configuration is missing.");
            defaultAvatarFormat = _configuration["UserService:DefaultAvatarFormat"]
                ?? throw new InvalidOperationException("CloudService:DefaultAvatarFormat configuration is missing.");
        }

        public async Task<string> SaveAvatarAsync(Stream avatarStream)
        {
            string fileName = Guid.NewGuid().ToString();
            string imagePath = $"{avatarPath}/{fileName}.{defaultAvatarFormat}";

            await _cloudService.UploadToR2(imagePath, avatarStream);
            await _imageRepository.Add(new Models.Image { Path = fileName, Extension = defaultAvatarFormat });

            return fileName;
        }

        public async Task<(Stream file, string contentType)> GetAvatarAsync(int avatarId)
        {
            var image = await _imageRepository.FindByIdAsync(avatarId);
            if (image == null)
            {
                throw new Exception("Avatar not found.");
            }
            return await _cloudService.GetFileStreamAsync($"{avatarPath}/{image.Path}.{image.Extension}");
        }
    }
}
