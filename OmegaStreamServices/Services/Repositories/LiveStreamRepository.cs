using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Repositories;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class InMemoryLiveStreamRepository : ILiveStreamRepository
{
    private readonly ConcurrentDictionary<string, LiveStream> _liveStreams = new ConcurrentDictionary<string, LiveStream>();

    public Task AddLiveStreamAsync(LiveStream liveStream)
    {
        _liveStreams[liveStream.Id] = liveStream;
        return Task.CompletedTask;
    }

    public Task<LiveStream> GetLiveStreamByIdAsync(string streamId)
    {
        _liveStreams.TryGetValue(streamId, out var liveStream);
        return Task.FromResult(liveStream);
    }

    public Task UpdateLiveStreamAsync(LiveStream liveStream)
    {
        _liveStreams[liveStream.Id] = liveStream;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<LiveStream>> GetAllLiveStreamsAsync()
    {
        var liveStreams = _liveStreams.Values.ToList();
        return Task.FromResult<IEnumerable<LiveStream>>(liveStreams);
    }

    public Task RemoveLiveStreamAsync(string streamId)
    {
        _liveStreams.TryRemove(streamId, out _);
        return Task.CompletedTask;
    }
}
