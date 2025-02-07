using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.VideoServices
{
    public interface IVideoLikeService
    {
        Task<string> IsUserLikedVideo(string userId, int videoId);
        Task<bool> UpdateUserLikedVideo(int videoId, string userId, string likeValue);

    }
}
