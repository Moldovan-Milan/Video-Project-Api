using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Text;
using Xabe.FFmpeg;
//using Xabe.FFmpeg.Downloader;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoUploadService : IVideoUploadService
    {
        private readonly IGenericRepository _repo;
        private readonly ICloudService _cloudServices;
        private readonly IConfiguration _configuration;

        private readonly string videoUploadPath;
        private readonly string thumbnailUploadPath;
        private readonly string defaultThumbnailFormat;

        private readonly int videoSplitTime = 30;
        private readonly int thumbnailSplitTime = 5;

        private readonly List<string> supportedImageFormats = new List<string> { "image/png", "image/jpg", "image/jpeg" };
        private readonly long maxThumbnailFileSize = 3145728; // 3 MB
        public VideoUploadService(ICloudService cloudServices, IConfiguration configuration, IGenericRepository repo)
        {
            _cloudServices = cloudServices;
            _configuration = configuration;

            videoUploadPath = _configuration["CloudService:VideoPath"]
                ?? throw new InvalidOperationException("CloudService:VideoPath configuration is missing.");

            thumbnailUploadPath = _configuration["CloudService:ThumbnailPath"]
                ?? throw new InvalidOperationException("CloudService:ThumbnailPath configuration is missing.");

            defaultThumbnailFormat = _configuration["VideoService:DefaultThumbnailFormat"]
                ?? throw new InvalidOperationException("VideoService:DefaultThumbnailFormat configuration is missing.");


            if (int.TryParse(_configuration["VideoService:VideoSplitTime"], out int vSplitTime))
                videoSplitTime = vSplitTime;
            if (int.TryParse(_configuration["VideoService:ThumbnailSplitTime"], out int tSplitTime))
                thumbnailSplitTime = tSplitTime;
            _repo = repo;
        }

        /// <summary>
        /// Uploads a video chunk and saves it to the temporary storage.
        /// </summary>
        /// <param name="chunk">The video chunk to be uploaded.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="chunkNumber">The chunk number.</param>
        public async Task UploadChunk(Stream chunk, string fileName, int chunkNumber)
        {
            if (!File.Exists($"{AppContext.BaseDirectory}/temp"))
            {
                FileManager.CreateDirectory($"{AppContext.BaseDirectory}/temp");
            }
            if (!File.Exists($"{AppContext.BaseDirectory}/temp/{fileName}"))
            {
                FileManager.CreateDirectory($"{AppContext.BaseDirectory}/temp/{fileName}");
            }
            var chunkPath = Path.Combine($"{AppContext.BaseDirectory}/temp/{fileName}", $"{fileName}.part{chunkNumber}");
            await FileManager.SaveStreamToFileAsync(chunkPath, chunk);
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
        public async Task AssembleFile(string fileName, Stream? image, int totalChunks, string title, string? description, string extension, string userId)
        {
            if (!File.Exists($"{AppContext.BaseDirectory}/temp"))
            {
                FileManager.CreateDirectory($"{AppContext.BaseDirectory}/temp");
            }
            // Generate a unique name for the video and thumbnail
            string uniqueFileName = FileManager.GenerateFileName();

            // Először egy külön mappát hoz létre
            FileManager.CreateDirectory($"{AppContext.BaseDirectory}/temp/{uniqueFileName}");
            // A készülő .mp4 fájl végleges útvonala
            var finalPath = Path.Combine($"{AppContext.BaseDirectory}/temp/{uniqueFileName}", $"{uniqueFileName}.{extension}");

            // Ha a fájl már létezik, akkor nem kell semmit sem csinálni
            if (File.Exists(finalPath))
            {
                return;
            }

            // Ez hozza létre az mp4 videót
            await AssembleAndSaveVideo(finalPath, fileName, $"{AppContext.BaseDirectory}/temp/{fileName}", totalChunks);


            await SaveImageToDatabase(uniqueFileName, defaultThumbnailFormat);

            var (width, height, duration) = await GetVideoPropertiesAsync($"{finalPath}");
            if (width == 0 || height == 0 || duration == TimeSpan.Zero)
            {
                return;
            }
            if(description == null)
            {
                description = "Teszt";
            }
            await SaveVideoToDatabase(uniqueFileName, duration, extension, title, description, userId, width, height);

            // Ha nincs indexkép, akkor készítünk egyet
            if (image == null)
            {
                int splitTime = duration.TotalSeconds < thumbnailSplitTime ? 0 : thumbnailSplitTime; // sec < thSplitTime => 1st image from the video
                image = await VideoSplitter.GenerateThumbnailImage($"{uniqueFileName}.{extension}", $"{AppContext.BaseDirectory}/temp/{uniqueFileName}", splitTime);
            }

            // Átalakítja az mp4-et .m3u8 formátummá
            await VideoSplitter.SplitMP4ToM3U8($"{uniqueFileName}.{extension}", uniqueFileName, $"{AppContext.BaseDirectory}/temp/{uniqueFileName}", videoSplitTime);

            // Ha minden megvan, akkor feltöltük a fájlokat
            await UploadVideoToR2(uniqueFileName);
            await _cloudServices.UploadToR2($"{thumbnailUploadPath}/{uniqueFileName}.{defaultThumbnailFormat}", image);
        }

        public async Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title, string description, string userId,
            int width, int height)
        {
            bool isShort = height > width && duration <= new TimeSpan(0, 3, 0);

            Image? image = await _repo.FirstOrDefaultAsync<Image>(x => x.Path == uniqueFileName);
            if (image == null)
            {
                throw new InvalidOperationException("Image not found in the database.");
            }
            Video video = new Video
            {
                Path = uniqueFileName,
                Created = DateTime.Now,
                Duration = new TimeSpan(duration.Hours, duration.Minutes, duration.Seconds),
                Extension = videoExtension,
                Title = title,
                Description = description,
                ThumbnailId = image.Id,
                IsShort = isShort,
                UserId = userId
            };

            await _repo.AddAsync(video);
        }

        public async Task SaveImageToDatabase(string fileName, string extension)
        {
            Image image = new Image
            {
                Path = fileName,
                Extension = extension
            };
            await _repo.AddAsync(image);
        }

        public async Task UploadVideoToR2(string folderName)
        {
            // Egy tömböt ad vissza, amiben benne van minden fájl elérési útvonala, amit a mappa tartalmaz
            string[] files = Directory.GetFiles($"{AppContext.BaseDirectory}/temp/{folderName}", "*.*", SearchOption.AllDirectories);
            var uploadTasks = files.Select(async file =>
            {
                if (file.Contains(".ts") || file.Contains(".m3u8"))
                {
                    string key = $"{videoUploadPath}/{folderName}/{Path.GetFileName(file)}";
                    var fileContent = File.ReadAllBytes(file);
                    using var memoryStream = new MemoryStream(fileContent);
                    await _cloudServices.UploadToR2(key, memoryStream);
                    FileManager.DeleteFile(file);
                }
                else if (file.Contains(".mp4"))
                {
                    FileManager.DeleteFile(file);
                }
            });

            await Task.WhenAll(uploadTasks);

            // Ha minden megvan, akkor kitörli a mappát, amiben a .mp4 és .ts fájlok voltak
            FileManager.DeleteDirectory($"{AppContext.BaseDirectory}/temp/{folderName}");
        }

        private async Task AssembleAndSaveVideo(string path, string fileName, string tempPath, int totalChunkCount)
        {
            try
            {
                using (var finalStream = new FileStream(path, FileMode.Create))
                {
                    for (int i = 0; i < totalChunkCount; i++)
                    {
                        // Az ideiglenes chunk elérési útvonala
                        // Ha lemásolta, akkor utána lerörli
                        var chunkPath = Path.Combine(tempPath, $"{fileName}.part{i}");
                        using var chunkStream = FileManager.OpenFileStream(chunkPath);
                        await chunkStream.CopyToAsync(finalStream);
                        chunkStream.Dispose();
                        FileManager.DeleteFile(chunkPath);
                    }
                    FileManager.DeleteDirectory(tempPath);
                }
            }
            catch (IOException ex)
            {
                throw;
            }
        }

        public async Task<(int width, int height, TimeSpan duration)> GetVideoPropertiesAsync(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Console.WriteLine($"The file does not exist at: {path}");
                    return (0, 0, TimeSpan.Zero);
                }

                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = $"-i \"{path}\"",
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = processStartInfo };
                StringBuilder output = new StringBuilder();

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        output.AppendLine(e.Data);
                };

                process.Start();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync();

                string ffmpegOutput = output.ToString();

                var resolutionMatch = Regex.Match(ffmpegOutput, @"(\d{2,5})x(\d{2,5})");
                var durationMatch = Regex.Match(ffmpegOutput, @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})");

                if (resolutionMatch.Success && durationMatch.Success)
                {
                    int width = int.Parse(resolutionMatch.Groups[1].Value);
                    int height = int.Parse(resolutionMatch.Groups[2].Value);

                    int hours = int.Parse(durationMatch.Groups[1].Value);
                    int minutes = int.Parse(durationMatch.Groups[2].Value);
                    int seconds = int.Parse(durationMatch.Groups[3].Value);
                    int milliseconds = int.Parse(durationMatch.Groups[4].Value) * 10;

                    var duration = new TimeSpan(0, hours, minutes, seconds, milliseconds);
                    return (width, height, duration);
                }

                Console.WriteLine("Could not parse video properties.");
                return (0, 0, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error happened: {ex.Message}");
                return (0, 0, TimeSpan.Zero);
            }
        }

        public async Task<bool> CanUploadVideo(long fileSize)
        {
            long totalSize = await _cloudServices.GetBucketFileSizeSum();
            string? bucketMaxSize = _configuration["CloudService:MaxBucketSize"];
            long maxSize;

            if (bucketMaxSize != null)
            {
                maxSize = long.Parse(bucketMaxSize);
            }
            else
            {
                maxSize = 10737418240; // 10 GB
            }

            if (totalSize + fileSize > maxSize)
            {
                return false;
            }
            return true;
        }

        public async Task<(bool, string)> CanUploadThumbnail(IFormFile thumbnail)
        {
            if(!supportedImageFormats.Contains(thumbnail.Headers["Content-Type"].ToString()))
            {
                return (false, "Unsupported image format");
            }
            long fileSize = thumbnail.Length;
            if(fileSize > maxThumbnailFileSize)
            {
                return (false, $"File is larger than {maxThumbnailFileSize*1024*1024} MB");
            }
            long totalSize = await _cloudServices.GetBucketFileSizeSum();
            string? bucketMaxSize = _configuration["CloudService:MaxBucketSize"];
            long maxSize;

            if (bucketMaxSize != null)
            {
                maxSize = long.Parse(bucketMaxSize);
            }
            else
            {
                maxSize = 10737418240; // 10 GB
            }
            if (totalSize + fileSize > maxSize)
            {
                return (false, "There isn't enough storage on the server to store this image");
            }
            return (true, "");
        }
    }
}