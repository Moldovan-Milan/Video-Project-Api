using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public interface IImageService
    {
        Task<string> SaveImage(string cloudPath, Stream imageStream);
        Task<(Stream?, string? contentType)> GetImageStreamByPathAsync(string cloudPath, string path);
        Task<(Stream?, string? contentType)> GetImageStreamByIdAsync(string cloudPath, int id);
        Task<bool> ReplaceImage(string cloudPath, string imagePath, Stream image);
    }
}