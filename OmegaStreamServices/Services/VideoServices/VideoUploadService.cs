﻿using Amazon.Runtime.Internal.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using WMPLib;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoUploadService : IVideoUploadService
    {

        private readonly IVideoRepository _videoRepository;
        private readonly IImageRepository _imageRepository;
        private readonly ICloudService _cloudServices;
        private readonly IConfiguration _configuration;

        private readonly string videoUploadPath;
        private readonly string thumbnailUploadPath;
        private readonly string defaultThumbnailFormat;

        private readonly int videoSplitTime = 30;
        private readonly int thumbnailSplitTime = 5;

        public VideoUploadService(IVideoRepository videoRepository, IImageRepository imageRepository, ICloudService cloudServices, IConfiguration configuration)
        {
            _videoRepository = videoRepository;
            _imageRepository = imageRepository;
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
        public async Task AssembleFile(string fileName, Stream? image, int totalChunks, string title, string extension, string userId)
        {
            // Generate a unique name for the video and thumbnail
            string uniqueFileName = FileManager.GenerateFileName();

            // Először egy külön mappát hoz létre
            FileManager.CreateDirectory($"temp/{uniqueFileName}");
            // A készülő .mp4 fájl végleges útvonala
            var finalPath = Path.Combine($"temp/{uniqueFileName}", $"{uniqueFileName}.{extension}");

            // Ha a fájl már létezik, akkor nem kell semmit sem csinálni
            if (File.Exists(finalPath))
            {
                return;
            }

            // Ez hozza létre az mp4 videót
            await AssembleAndSaveVideo(finalPath, fileName, "temp", totalChunks);

            
            await SaveImageToDatabase(uniqueFileName, defaultThumbnailFormat);

            TimeSpan duration = GetVideoDuration(finalPath);
            await SaveVideoToDatabase(uniqueFileName, duration, extension, title, userId);

            // Ha nincs indexkép, akkor készítünk egyet
            if (image == null)
            {
                int splitTime = duration.TotalSeconds < thumbnailSplitTime ? 0 : thumbnailSplitTime; // sec < thSplitTime => 1st image from the video
                image = await VideoSplitter.GenerateThumbnailImage($"{uniqueFileName}.{extension}", $"temp/{uniqueFileName}", splitTime);
            }

            // Átalakítja az mp4-et .m3u8 formátummá
            await VideoSplitter.SplitMP4ToM3U8($"{uniqueFileName}.{extension}", uniqueFileName, $"temp/{uniqueFileName}", videoSplitTime);

            // Ha minden megvan, akkor feltöltük a fájlokat
            await UploadVideoToR2(uniqueFileName);
            await _cloudServices.UploadToR2($"{thumbnailUploadPath}/{uniqueFileName}.{defaultThumbnailFormat}", image);
        }

        public async Task SaveVideoToDatabase(string uniqueFileName, TimeSpan duration, string videoExtension, string title, string userId)
        {
            Image image = await _imageRepository.FindImageByPath(uniqueFileName);
            Video video = new Video
            {
                Path = uniqueFileName,
                Created = DateTime.Now,
                Duration = duration,
                Extension = videoExtension,
                Title = title,
                Description = "Teszt",
                ThumbnailId = image.Id,
                UserId = userId
            };

            await _videoRepository.Add(video);
        }

        public async Task SaveImageToDatabase(string fileName, string extension)
        {
            Image image = new Image
            {
                Path = fileName,
                Extension = extension
            };
            await _imageRepository.Add(image);
        }

        public async Task UploadVideoToR2(string folderName)
        {
            // Egy tömböt ad vissza, amiben benne van minden fájl elérési útvonala, amit a mappa tartalmaz
            string[] files = Directory.GetFiles($"temp/{folderName}", "*.*", SearchOption.AllDirectories);
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
            FileManager.DeleteDirectory($"temp/{folderName}");
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
                }
            }
            catch (IOException ex)
            {
                throw;
            }
        }

        private TimeSpan GetVideoDuration(string path)
        {
            WindowsMediaPlayer wmp = new WindowsMediaPlayer();
            IWMPMedia mediaInfo = wmp.newMedia(path);
            return TimeSpan.FromSeconds(mediaInfo.duration);
        }
    }
}
