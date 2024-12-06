using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using System.IO;
using WMPLib;

namespace OmegaStreamServices.Services
{
    public class FileManagerService : IFileManagerService
    {
        /// <summary>
        /// Assembles video chunks into a single video file and saves it to the specified path.
        /// </summary>
        /// <param name="path">The final path where the assembled video will be saved.</param>
        /// <param name="tempPath">The temporary path where the video chunks are stored.</param>
        /// <param name="totalChunkCount">The total number of chunks to be assembled.</param>
        /// 
        private readonly string _accessKey;
        private readonly string _secretKey;
        private readonly string _serviceUrl;
        private readonly string _bucketName;

        private readonly BasicAWSCredentials _credentials;
        private readonly AmazonS3Config _awsConfig;

        public FileManagerService(IConfiguration configuration)
        {
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

        public async Task AssembleAndSaveVideo(string path, string fileName, string tempPath, int totalChunkCount)
        {
            using (var finalStream = new FileStream(path, FileMode.Create))
            {
                for (int i = 0; i < totalChunkCount; i++)
                {
                    var chunkPath = Path.Combine(tempPath, $"{fileName}.part{i}");
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        await chunkStream.CopyToAsync(finalStream);
                    }
                    File.Delete(chunkPath);
                }
            }
        }

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The path of the file to be deleted.</param>
        public void DeleteFile(string path)
        {
            File.Delete(path);
        }

        /// <summary>
        /// Generates a unique file name using a GUID.
        /// </summary>
        /// <returns>A unique file name.</returns>
        public string GenerateFileName()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets the duration of the video at the specified path.
        /// </summary>
        /// <param name="path">The path of the video file.</param>
        /// <returns>The duration of the video.</returns>
        public TimeSpan GetVideoDuration(string path)
        {
            WindowsMediaPlayer wmp = new WindowsMediaPlayer();
            IWMPMedia mediaInfo = wmp.newMedia(path);
            return TimeSpan.FromSeconds(mediaInfo.duration);
        }

        /// <summary>
        /// Saves the image to the specified path.
        /// </summary>
        /// <param name="path">The path where the image will be saved.</param>
        /// <param name="image">The image file to be saved.</param>
        public async Task SaveImage(string path, Stream image)
        {
            //using var stream = new FileStream(path, FileMode.Create);
            //image.CopyTo(stream);
            AmazonS3Client client = new AmazonS3Client(_credentials, _awsConfig);

            var request = new PutObjectRequest
            {
                BucketName = "omega-stream",
                Key = path,
                InputStream = image,
                ContentType = "image/png",
                DisablePayloadSigning = true
            };

            var response = await client.PutObjectAsync(request);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK && response.HttpStatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception("Upload to Cloudflare R2 failed");
            }
        }

        /// <summary>
        /// Saves a video chunk to the specified path.
        /// </summary>
        /// <param name="path">The path where the chunk will be saved.</param>
        /// <param name="chunk">The video chunk to be saved.</param>
        /// <param name="chunkNumber">The chunk number.</param>
        public async Task SaveVideoChunk(string path, Stream chunk, int chunkNumber)
        {
            using (FileStream stream = new FileStream(path, FileMode.Create))
            {
               await chunk.CopyToAsync(stream); // Elmenti a chunk-ot az adott videó fájlba
            }
        }

        /// <summary>
        /// Get FileStream for a specifed file
        /// </summary>
        /// <param name="path">The path where the file stored.</param>
        /// <returns>A FileStream for the file</returns>

        public FileStream GetFileStream(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void CreateDirectory(string path)
        {
            try
            {
                // Determine whether the directory exists.
                if (Directory.Exists(path))
                {
                    return;
                }

                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Console.WriteLine("The process failed: {0}", e.ToString());
            }
        }

        public async void SplitMP4ToM3U8(string inputPath, string outputName, string workingDirectory, int splitTimeInSec = 10)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "ffmpeg";
                process.StartInfo.Arguments =
                    $"-i \"{inputPath}\" -codec: copy -start_number 0 -hls_time {splitTimeInSec} -hls_list_size 0 -hls_segment_filename \"{outputName}%03d.ts\" -f hls \"{outputName}.m3u8\"";
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.OutputDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine(e.Data); };
                process.ErrorDataReceived += (sender, e) => { if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"ERROR: {e.Data}"); };

                process.Start();

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();
                var lines = ReadAndChange($"{workingDirectory}/{outputName}.m3u8");
                WriteM3U8File(lines, $"{outputName}.m3u8");
                await UploadVideoToR2($"{workingDirectory}/{outputName}");
            }
            
        }

        public List<string> ReadAndChange(string inputFileName)
        {
            List<string> result = new List<string>();
            StreamReader streamReader = new StreamReader(inputFileName);

            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                if (line.Contains(".ts"))
                {
                    string changedLine = $"/semgments/{line}";
                    result.Add(changedLine);
                }
                else
                {
                    result.Add(line);
                }
            }

            return result;
        }
        public void WriteM3U8File(List<string> lines, string fileName)
        {
            StreamWriter streamWriter = new StreamWriter(fileName);
            foreach (string line in lines)
            {
                streamWriter.WriteLine(line);
            }
            streamWriter.Close();
        }

        public async Task UploadVideoToR2(string folderName)
        {
            var files = Directory.GetFiles(folderName, "*.*", SearchOption.AllDirectories);
        }
    }
}
