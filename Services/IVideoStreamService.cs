using Microsoft.AspNetCore.Mvc;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Services
{
    public interface IVideoStreamService
    {
        AppDbContext Context { get; }
        string VideoPath { get; }
        Task<List<Video>> GetAllVideosData();
        Task<Video> GetVideoData(int id);
        FileStream StreamVideo(Video video);
        FileStream GetThumbnailImage(string name);
    }
}
