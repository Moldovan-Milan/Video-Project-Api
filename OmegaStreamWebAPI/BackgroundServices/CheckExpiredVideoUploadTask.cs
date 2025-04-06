
using OmegaStreamServices.Data;
using OmegaStreamServices.Services.Repositories;

namespace OmegaStreamWebAPI.BackgroundServices
{
    public class CheckExpiredVideoUploadTask : BackgroundService
    {
        private readonly IUserVideoUploadRepositroy _userVideoUploadRepository;

        public CheckExpiredVideoUploadTask(IUserVideoUploadRepositroy userVideoUploadRepository)
        {
            _userVideoUploadRepository = userVideoUploadRepository;
        }

        /// <summary>
        /// Executes the background service to check for expired video uploads.
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while(!stoppingToken.IsCancellationRequested)
            {
                var userVideoUploads = await _userVideoUploadRepository.GetAllUserVideoUploads();

                foreach (var userVideoUpload in userVideoUploads)
                {
                    if (userVideoUpload.ExpirationDate < DateTime.UtcNow)
                    {
                        Console.WriteLine($"Video upload (name: {userVideoUpload.VideoName}, userId: {userVideoUpload.UserId}) is expired ({userVideoUpload.ExpirationDate})");
                        RemoveVideoFromTemp(userVideoUpload.VideoName);
                        await _userVideoUploadRepository.RemoveUserVideoUpload(userVideoUpload.UserId);
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        /// <summary>
        /// Removes the video from temporary storage.
        /// </summary>
        /// <param name="videoName"></param>
        /// <returns></returns>
        private static void RemoveVideoFromTemp(string videoName)
        {
            Console.WriteLine("Delete video files from temp");
            string videoPath = Path.Combine(AppContext.BaseDirectory, "temp", videoName);
            FileManager.DeleteDirectoryWithFiles(videoPath);
        }
    }
}
