

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Data;
using VideoProjektAspApi.Model;
using WMPLib;

namespace VideoProjektAspApi.Services
{
    public class VideoUploadService : IVideoUploadService
    {
        public string VideoPath { get; private set; }
        public AppDbContext Context { get; private set; }
        public string TempPath { get; private set; }

        public VideoUploadService(AppDbContext context)
        {
            Context = context;
            VideoPath = Path.Combine("video");
            TempPath = Path.Combine("temp");
        }

        public async Task UploadChunk(IFormFile chunk, string fileName, int chunkNumber)
        {
            var chunkPath = Path.Combine(TempPath, $"{fileName}.part{chunkNumber}");
            using (FileStream stream = new FileStream(chunkPath, FileMode.Create))
            {
                await chunk.CopyToAsync(stream); // Átmásolja a chunk tartalmát a fájlba.
            }
        }

        public async Task AssembleFile(string fileName, IFormFile image, int totalChunks, string title, string extension)
        {
            // Egyedi név adása a videónak és az indexképnek
            string uniqueFileName = GenerateUniqueFileName();
            var finalPath = Path.Combine(VideoPath, $"{uniqueFileName}.{extension}");

            await AssembleChunksToFile(finalPath, fileName, totalChunks);
            SaveThumbnail(image, uniqueFileName);

            TimeSpan duration = GetVideoDuration(finalPath);
            await SaveVideoToDatabase(uniqueFileName, duration, extension, title);
        }

        public async Task AssembleChunksToFile(string finalPath, string fileName, int totalChunks)
        {
            using (var finalStream = new FileStream(finalPath, FileMode.Create))
            {
                for (int i = 0; i < totalChunks; i++)
                {
                    var chunkPath = Path.Combine(TempPath, $"{fileName}.part{i}");
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await chunkStream.CopyToAsync(finalStream);
                    }
                    System.IO.File.Delete(chunkPath);
                }
            }
        }

        public bool ConvertThumbnailToJpg(IFormFile image)
        {
            throw new NotImplementedException();
        }

        public string GenerateUniqueFileName()
        {
            string uniqueFileName;
            do
            {
                uniqueFileName = Guid.NewGuid().ToString();
            } while (Context.Videos.Any(x => x.Path == uniqueFileName));
            return uniqueFileName;
        }

        public TimeSpan GetVideoDuration(string finalPath)
        {
            WindowsMediaPlayer wmp = new WindowsMediaPlayer();
            IWMPMedia mediaInfo = wmp.newMedia(finalPath);
            return TimeSpan.FromSeconds(mediaInfo.duration);
        }

        public void SaveThumbnail(IFormFile image, string uniqueFileName)
        {
            if (image != null && image.Length > 0)
            {
                var imagePath = Path.Combine("video/thumbnail/", $"{uniqueFileName}.png");
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    image.CopyTo(stream);
                }
            }
        }

        public async Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title)
        {
            Video video = new Video
            {
                Path = uniqueFileName,
                Created = DateTime.Now,
                Duration = duration,
                Extension = videoExtension,
                ThumbnailPath = uniqueFileName,
                Title = title
            };

            await Context.Videos.AddAsync(video);
            await Context.SaveChangesAsync();
        }
    }
}
