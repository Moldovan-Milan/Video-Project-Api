using VideoProjektAspApi.Data;

namespace VideoProjektAspApi.Services
{
    public interface IVideoUploadService
    {
        string VideoPath { get; }
        AppDbContext Context { get; }
        string TempPath { get; }

        Task UploadChunk(IFormFile chunk, string fileName, int chunkNumber);

        Task AssembleFile(string fileName, IFormFile image, int totalChunks, string title, 
            string extension);

        Task AssembleChunksToFile(string finalPath, string fileName, int totalChunks);

        void SaveThumbnail(IFormFile image, string uniqueFileName);

        string GenerateUniqueFileName();

        TimeSpan GetVideoDuration(string finalPath);

        Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title);

        // Elmenti az indexképet jpg formátumba
        // TODO: Megcsinálni a jpeg konvertálást
        bool ConvertThumbnailToJpg(IFormFile image);
    }
}
