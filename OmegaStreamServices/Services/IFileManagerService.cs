using Microsoft.AspNetCore.Http;

namespace OmegaStreamServices.Services
{
    public interface IFileManagerService
    {
        string GenerateFileName();
        Task SaveImage(string path, Stream image);
        void DeleteFile(string path);
        Task SaveVideoChunk(string path, Stream chunk, int chunkNumber);
        Task AssembleAndSaveVideo(string path, string fileName, string tempPath, int totalChunkCount);
        FileStream GetFileStream(string path);
        TimeSpan GetVideoDuration(string path);
        void CreateDirectory(string path);
        void SplitMP4ToM3U8(string inputPath, string outputName, string workingDirectory, int splitTimeInSec = 10);
        List<string> ReadAndChange(string inputFileName);
        void WriteM3U8File(List<string> lines, string fileName);
        Task UploadVideoToR2(string folderName);
    }
}
