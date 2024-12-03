using Microsoft.AspNetCore.Http;

namespace OmegaStreamServices.Services
{
    public interface IFileManagerService
    {
        string GenerateFileName();
        void SaveImage(string path, Stream image);
        void DeleteFile(string path);
        Task SaveVideoChunk(string path, Stream chunk, int chunkNumber);
        Task AssembleAndSaveVideo(string path, string fileName, string tempPath, int totalChunkCount);
        FileStream GetFileStream(string path);
        TimeSpan GetVideoDuration(string path);
        
    }
}
