using OmegaStreamServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoViewService
    {
        Task<List<VideoView>> getUserViewHistory(string userId)
        Task<bool> ValidateView(VideoView view);
    }
}
