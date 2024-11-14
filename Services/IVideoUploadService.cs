using VideoProjektAspApi.Data;

namespace VideoProjektAspApi.Services
{
    public interface IVideoUploadService
    {
        Task UploadChunk(IFormFile chunk, string fileName, int chunkNumber);

        Task AssembleFile(string fileName, IFormFile image, int totalChunks, string title,
            string extension, string userId);

        Task SaveImageToDatabase(string fileName, string extension);

        Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title, string userId);
    }
}
