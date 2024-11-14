namespace VideoProjektAspApi.Services
{
    public interface IFileManagerService
    {
        string GenerateFileName();
        void SaveImage(string path, IFormFile image);
        void DeleteFile(string path);
        Task SaveVideoChunk(string path, IFormFile chunk, int chunkNumber);
        Task AssembleAndSaveVideo(string path, string fileName, string tempPath, int totalChunkCount);
        FileStream GetFileStream(string path);
        TimeSpan GetVideoDuration(string path);
        
    }
}
