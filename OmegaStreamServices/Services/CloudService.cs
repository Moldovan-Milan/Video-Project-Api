using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using OmegaStreamServices.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services
{
    public class CloudService : ICloudService
    {
        private readonly R2Settings _settings;
        private readonly BasicAWSCredentials _credentials;
        private readonly AmazonS3Config _awsConfig;
        private readonly AmazonS3Client _client;

        public CloudService(IOptions<R2Settings> options, bool forcePathStyle = true)
        {
            // R2 beállítása
            _settings = options.Value;

            _credentials = new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey);
            _awsConfig = new AmazonS3Config
            {
                ServiceURL = _settings.ServiceUrl,
                ForcePathStyle = forcePathStyle,

            };
            _client = new AmazonS3Client(_credentials, _awsConfig);
        }

        public async Task<(Stream stream, string contentType)> GetFileStreamAsync(string key)
        {
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
            };
            var response = await _client.GetObjectAsync(request);
            return (response.ResponseStream, response.Headers["Content-Type"]);
        }

        public async Task UploadToR2(string key, Stream content)
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = content,
                DisablePayloadSigning = true
            };
            var response = await _client.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to upload {key} to S3.");
            }
        }
    }
}
