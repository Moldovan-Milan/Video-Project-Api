using Microsoft.AspNetCore.Http;

namespace OmegaStreamServices.Services
{
    public interface IFileManagerService
    {
        Task SaveStreamToFileAsync(string path, Stream content);
        FileStream OpenFileStream(string path);
        void DeleteFile(string path);
        void CreateDirectory(string path);
        void DeleteDirectory(string path);
        string GenerateFileName();
    }
}
