using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Services
{
    public class VideoStreamService : IVideoStreamService
    {
        public AppDbContext Context { get;  private set; }

        public string VideoPath { get; private set; }

        public VideoStreamService(AppDbContext context) 
        { 
            Context = context;
            VideoPath = Path.Combine("video");
        }

        public async Task<List<Video>> GetAllVideosData()
        {
            return await Context.Videos.ToListAsync(); ;
        }

        public FileStream GetThumbnailImage(string name)
        {
            string fullPath = Path.Combine("video/thumbnail", $"{name}.png");
            FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                   FileAccess.Read, FileShare.Read);
            return fileStream;
        }

        public async Task<Video> GetVideoData(int id)
        {
            return await Context.Videos.FirstOrDefaultAsync(v => v.Id == id);
        }

        public FileStream StreamVideo(Video video)
        {
            string fullPath = Path.Combine(VideoPath, $"{video.Path}.{video.Extension}");
                FileStream fileStream = new FileStream(fullPath, FileMode.Open,
                    FileAccess.Read, FileShare.Read);

            return fileStream;
        }
    }
}
