using System.Threading.Tasks;
using System.Collections.Generic;
using OmegaStreamServices.Models;

namespace OmegaStreamServices.Services.Repositories
{
    public interface ILiveStreamRepository
    {
        Task AddLiveStreamAsync(LiveStream liveStream);
        Task<LiveStream> GetLiveStreamByIdAsync(string streamId);
        Task UpdateLiveStreamAsync(LiveStream liveStream);
        Task<IEnumerable<LiveStream>> GetAllLiveStreamsAsync();
    }
}
