using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface ISubscriptionRepository: IBaseRepository<Subscription>
    {
        Task<List<string>?> GetUserSubscribesById(string followerId);
        Task<bool> IsUserSubscribedToChanel(string followerId, string followedId);
        Task<Subscription?> GetByIdAsync(string followerId, string followedId);
        Task<bool> IsUserAndChanelExist(string followerId, string folowedId);
        Task RemoveUserSubscribeFromChanel(string followerId, string followedId);
        Task SubscribeUserToChanel(string followerId, string followedId);
        Task<int> GetFollowersCount(string userId);
    }
}
