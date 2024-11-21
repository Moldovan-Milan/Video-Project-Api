using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Services
{
    public class VideoStreamService : IVideoStreamService
    {
        private readonly AppDbContext _context;
        private readonly IFileManagerService _fileManagerService;

        public VideoStreamService(AppDbContext context, IFileManagerService fileManagerService)
        {
            _context = context;
            _fileManagerService = fileManagerService;
        }

        public async Task<List<Video>> GetAllVideosData()
        {
            return await _context.Videos.ToListAsync();
        }

        public async Task<FileStream> GetThumbnailImage(int id)
        {
            Image image = await _context.Images.FirstOrDefaultAsync(i => i.Id == id);
            if (image == null)
                return null;

            string fullPath = Path.Combine("video/thumbnail", $"{image.Path}.{image.Extension}");
            return _fileManagerService.GetFileStream(fullPath);
        }

        public async Task<Video> GetVideoData(int id)
        {
            var video = await _context.Videos.Include(v => v.User).ThenInclude(u => u.Avatar).FirstOrDefaultAsync(v => v.Id == id); 
            if (video != null) 
            { 
                User user = new User
                {
                    UserName = video.User.UserName,
                    Avatar = video.User.Avatar,
                    Followers = video.User.Followers
                };
                video.User = user;
            }
            return video;

        }

        public FileStream StreamVideo(Video video)
        {
            string fullPath = Path.Combine("video", $"{video.Path}.{video.Extension}");
            return _fileManagerService.GetFileStream(fullPath);
        }
    }
}
