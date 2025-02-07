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

        public AvatarService(ICloudService cloudService, IImageRepository imageRepository)
        {
            _cloudService = cloudService;
            _imageRepository = imageRepository;
        }

        public async Task<string> SaveAvatarAsync(Stream avatarStream)
        {
            string fileName = Guid.NewGuid().ToString();
            string imagePath = Path.Combine("images/avatars/", $"{fileName}.png");

            await _cloudService.UploadToR2(imagePath, avatarStream);
            await _imageRepository.Add(new Models.Image { Path = fileName, Extension = "png" });

            return fileName;
        }

        public async Task<(Stream file, string contentType)> GetAvatarAsync(int avatarId)
        {
            var image = await _imageRepository.FindByIdAsync(avatarId);
            if (image == null)
            {
                throw new Exception("Avatar not found.");
            }
            return await _cloudService.GetFileStreamAsync($"images/avatars/{image.Path}.{image.Extension}");
        }
    }
}
