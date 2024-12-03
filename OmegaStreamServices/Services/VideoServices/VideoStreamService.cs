using Amazon.Runtime;
using Amazon.S3.Model;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using Microsoft.EntityFrameworkCore;

namespace OmegaStreamServices.Services.VideoServices
{
    public class VideoStreamService : IVideoStreamService
    {
        private readonly AppDbContext _context;

        // Paraméterei az R2 Object Storage-nak
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _serviceUrl;
        private readonly string _bucketName;

        private readonly BasicAWSCredentials _credentials;
        private readonly AmazonS3Config _awsConfig;

        public VideoStreamService(IConfiguration configuration, AppDbContext context)
        {
            // Db conn
            _context = context;

            // R2 beállítása
            _accessKey = configuration["R2:AccessKey"]!;
            _secretKey = configuration["R2:SecretKey"]!;
            _serviceUrl = configuration["R2:ServiceUrl"]!;
            _bucketName = configuration["R2:BucketName"]!;
            _credentials = new BasicAWSCredentials(_accessKey, _secretKey);
            _awsConfig = new AmazonS3Config
            {
                ServiceURL = _serviceUrl,
                ForcePathStyle = true,

            };
        }

        #region Stream
        public async Task<(Stream imageStream, string contentType)> GetImageStreamAsync(int imageId, string path = "thumbnails")
        {
            AmazonS3Client _awsClient = new AmazonS3Client(_credentials, _awsConfig);
            // Kép elérési útvonalának megkeresése
            Image image = await _context.Images.FirstOrDefaultAsync(x => x.Id == imageId);


            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = $"images/{path}/{image.Path}.{image.Extension}",
            };
            var response = await _awsClient.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers["Content-Type"] ?? "image/png");
        }

        public async Task<(Stream segmentStream, string contentType)> GetVideoSegmentAsync(string segmentKey)
        {
            AmazonS3Client _awsClient = new AmazonS3Client(_credentials, _awsConfig);
            string path = GetFolder(segmentKey);
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = $"videos/{path}/{segmentKey}"
            };

            var response = await _awsClient.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers["Content-Type"] ?? "video/mp2t");
        }


        public async Task<(Stream videoStream, string contentType)> GetVideoStreamAsync(string videoKey)
        {
            AmazonS3Client _awsClient = new AmazonS3Client(_credentials, _awsConfig);
            var path = videoKey.Split(".").First();
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = $"videos/{path}/{videoKey}"
            };

            var response = await _awsClient.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers["Content-Type"] ?? "application/vnd.apple.mpegurl");
        }

        private string GetFolder(string fileName)
        {
            // Kiterjesztés eltávolítása
            int lastPeriodIndex = fileName.LastIndexOf('.');
            if (lastPeriodIndex == -1)
            {
                lastPeriodIndex = fileName.Length; // Ha nincs kiterjesztés, akkor az egész fájlnév az azonosító
            }

            // Az utolsó három számjegy eltávolítása
            string baseName = fileName.Substring(0, lastPeriodIndex); // Az utolsó három számot és a kiterjesztést levágjuk
            int lengthWithoutSuffix = baseName.Length - 3;

            return baseName.Substring(0, lengthWithoutSuffix);
        }

        #endregion Stream


        #region MetaData
        public async Task<List<Video>> GetAllVideosMetaData()
        {
            return await _context.Videos.ToListAsync();
        }

        public async Task<Video> GetVideoMetaData(int id)
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
        #endregion
    }
}
