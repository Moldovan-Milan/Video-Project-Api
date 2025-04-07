using System.Threading.Tasks;
using System.Collections.Generic;
using OmegaStreamServices.Models;

namespace OmegaStreamServices.Services.Repositories
{
    public interface ILiveStreamRepository
    {
        Task AddLiveStreamAsync(LiveStream liveStream);
        Task<LiveStream?> GetLiveStreamByIdAsync(string streamId);
        Task<LiveStream?> GetLiveStreamByUserIdAsync(string userId);
        Task UpdateLiveStreamAsync(LiveStream liveStream);
        Task<IEnumerable<LiveStream>> GetAllLiveStreamsAsync();
        Task RemoveLiveStreamAsync(string streamId);
        Task<LiveStream?> GetLiveStreamByConnectionIdAsync(string connectionId);
    }
}
