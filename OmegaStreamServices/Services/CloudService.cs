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
        }

        public async Task<(Stream stream, string contentType)> GetFileStreamAsync(string key)
        {
            var client = new AmazonS3Client(_credentials, _awsConfig);
            var request = new GetObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
            };
            var response = await client.GetObjectAsync(request);
            client.Dispose();
            return (response.ResponseStream, response.Headers["Content-Type"]);
        }

        public async Task UploadToR2(string key, Stream content)
        {
            var client = new AmazonS3Client(_credentials, _awsConfig);
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = content,
                DisablePayloadSigning = true
            };
            var response = await client.PutObjectAsync(request);

            client.Dispose();
            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception($"Failed to upload {key} to S3.");
            }
        }

        public async Task DeleteFilesAsync(string path)
        {
            var client = new AmazonS3Client(_credentials, _awsConfig);
            try
            {
                var listRequest = new ListObjectsV2Request
                {
                    BucketName = _settings.BucketName,
                    Prefix = path
                };

                var listResponse = await client.ListObjectsV2Async(listRequest);

                if (listResponse.S3Objects.Count == 0)
                {
                    throw new Exception($"No files found: {path}");
                }

                foreach (var s3Object in listResponse.S3Objects)
                {
                    var deleteRequest = new DeleteObjectRequest
                    {
                        BucketName = _settings.BucketName,
                        Key = s3Object.Key
                    };
                    await client.DeleteObjectAsync(deleteRequest);
                }

                Console.WriteLine($"Successfully deleted all files: {path}");
                client.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting files {path}: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string filePath)
        {
            var client = new AmazonS3Client(_credentials, _awsConfig);
            try
            {
                var deleteRequest = new DeleteObjectRequest
                {
                    BucketName = _settings.BucketName,
                    Key = filePath
                };
                await client.DeleteObjectAsync(deleteRequest);

                Console.WriteLine($"Successfully deleted file: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                throw;
            }
            client.Dispose();
        }

        public async Task<long> GetBucketFileSizeSum()
        {
            long totalSize = 0;
            var client = new AmazonS3Client(_credentials, _awsConfig);
            try
            {
                ListObjectsV2Response listRequest = await client.ListObjectsV2Async(new ListObjectsV2Request
                {
                    BucketName = _settings.BucketName
                });

                totalSize = listRequest.S3Objects.Sum(o => o.Size);
                Console.WriteLine($"Total size of all files in bucket: {totalSize} bytes");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Error getting bucket file size sum: {ex.Message}");
                throw;
            }
            finally
            {
                client.Dispose();
            }
            return totalSize;
        }
    }
}
