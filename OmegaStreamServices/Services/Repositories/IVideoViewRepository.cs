using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IVideoViewRepository: IBaseRepository<VideoView>
    {
        List<VideoView> GuestViews { get; set; }
        Task<List<VideoView>> GetUserViewHistory(string userId, int pageNumber, int pageSize);
        void RemoveOutdatedGuestViews();
        void AddGuestView(VideoView view);
        Task<VideoView> GetLastUserVideoView(string userId, int videoId);
        Task AddLoggedInVideoView(VideoView view);
    }
}
