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
        public void SaveImage(string path, Stream image)
        {
            using var stream = new FileStream(path, FileMode.Create);
            image.CopyTo(stream);
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

    }
}
