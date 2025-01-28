using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IVideoLikesRepository: IBaseRepository<VideoLikes>
    {
        Task<int> GetLikesByVideoId(int  videoId);
        Task<int> GetDisLikesByVideoId(int videoId);
        Task<string> IsLikedByUser(string userId, int videoId);
        Task<VideoLikes> GetVideoLike (string userId, int videoId);
    }
}
