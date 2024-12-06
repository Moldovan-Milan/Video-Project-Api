using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using WMPLib;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoUploadService : IVideoUploadService
    {
        private readonly AppDbContext _context;
        private readonly IFileManagerService _fileManagerService;

        public VideoUploadService(AppDbContext context, IFileManagerService fileManagerService)
        {
            _context = context;
            _fileManagerService = fileManagerService;
        }

        /// <summary>
        /// Uploads a video chunk and saves it to the temporary storage.
        /// </summary>
        /// <param name="chunk">The video chunk to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="chunkNumber">The chunk number.</param>
        public async Task UploadChunk(Stream chunk, string fileName, int chunkNumber)
        {
            var chunkPath = Path.Combine("temp", $"{fileName}.part{chunkNumber}");
            await _fileManagerService.SaveVideoChunk(chunkPath, chunk, chunkNumber);
        }

        /// <summary>
        /// Assembles the video chunks into a single file and saves the video and its metadata to the database.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="image">The thumbnail image.</param>
        /// <param name="totalChunks">The total number of chunks.</param>
        /// <param name="title">The title of the video.</param>
        /// <param name="extension">The file extension of the video.</param>
        /// <param name="userId">The ID of the user who uploaded the video.</param>
        public async Task AssembleFile(string fileName, Stream image, int totalChunks, string title, string extension, string userId)
        {
            // Generate a unique name for the video and thumbnail
            string uniqueFileName = _fileManagerService.GenerateFileName();

            _fileManagerService.CreateDirectory($"temp/{uniqueFileName}");
            var finalPath = Path.Combine($"temp/{uniqueFileName}", $"{uniqueFileName}.{extension}");

            
            await _fileManagerService.AssembleAndSaveVideo(finalPath, fileName, "temp", totalChunks);
            await _fileManagerService.SaveImage(Path.Combine("images/thumbnail/", $"{uniqueFileName}.png"), image);
            await SaveImageToDatabase(uniqueFileName, "png");

            // Convert mp4 into m3u8
            _fileManagerService.SplitMP4ToM3U8(finalPath, uniqueFileName, $"temp/{uniqueFileName}", 20);
            

            TimeSpan duration = _fileManagerService.GetVideoDuration(finalPath);
            await SaveVideoToDatabase(uniqueFileName, duration, extension, title, userId);
        }

        



        /// <summary>
        /// Saves the video metadata to the database.
        /// </summary>
        /// <param name="uniqueFileName">The unique file name of the video.</param>
        /// <param name="duration">The duration of the video.</param>
        /// <param name="videoExtension">The file extension of the video.</param>
        /// <param name="title">The title of the video.</param>
        /// <param name="userId">The ID of the user who uploaded the video.</param>
        public async Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title, string userId)
        {
            Video video = new Video
            {
                Path = uniqueFileName,
                Created = DateTime.Now,
                Duration = duration,
                Extension = videoExtension,
                Title = title,
                Dislikes = 0,
                Likes = 0,
                Status = "none",
                Description = "Teszt",
                ThumbnailId = _context.Images.FirstOrDefault(i => i.Path == uniqueFileName).Id,
                UserId = userId
            };

            await _context.Videos.AddAsync(video);
            await _context.SaveChangesAsync();
        }

        public async Task SaveImageToDatabase(string fileName, string extension)
        {
            Image image = new Image
            {
                Path = fileName,
                Extension = extension
            };
            await _context.Images.AddAsync(image);
            await _context.SaveChangesAsync();
        }
    }
}
