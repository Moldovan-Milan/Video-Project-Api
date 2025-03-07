using OmegaStreamServices.Models;
using OmegaStreamServices.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoViewService
    {
        Task<List<VideoViewDto>> GetUserViewHistory(string userId);
        Task<bool> ValidateView(VideoView view);
    }
}
