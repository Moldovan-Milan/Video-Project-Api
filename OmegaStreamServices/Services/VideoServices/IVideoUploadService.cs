using Microsoft.AspNetCore.Http;
using OmegaStreamServices.Data;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoUploadService
    {
        Task UploadChunk(Stream chunk, string fileName, int chunkNumber);

        Task AssembleFile(string fileName, Stream? image, int totalChunks, string title,
            string extension, string userId);

        Task SaveImageToDatabase(string fileName, string extension);

        Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title, string userId);
        Task UploadVideoToR2(string folderName);
    }
}
