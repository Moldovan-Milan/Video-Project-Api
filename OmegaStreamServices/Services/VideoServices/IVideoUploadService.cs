using Microsoft.AspNetCore.Http;
using OmegaStreamServices.Data;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoUploadService
    {
        Task<bool> CanUploadVideo(long fileSize);
        Task<(bool, string)> CanUploadThumbnail(IFormFile thumbnail);
        Task UploadChunk(Stream chunk, string fileName, int chunkNumber);

        Task AssembleFile(string fileName, Stream? image, int totalChunks, string title, string? description,
            string extension, string userId);

        Task SaveImageToDatabase(string fileName, string extension);

        Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title, string description, string userId,
             int width, int height);
        Task UploadVideoToR2(string folderName);
    }
}
