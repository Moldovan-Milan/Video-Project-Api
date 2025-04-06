using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public class SubscriptionRepository: BaseRepository<Subscription>, ISubscriptionRepository
    {
        private readonly UserManager<User> _userManager;

        public SubscriptionRepository(AppDbContext context, UserManager<User> userManager): base(context)
        {
            _userManager = userManager;
        }

        public async Task<Subscription?> GetByIdAsync(string followerId, string followedId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.FollowerId == followerId && x.FollowedUserId == followedId);
        }

        public async Task<int> GetFollowersCount(string userId)
        {
            return await _dbSet.Where(x => x.FollowedUserId == userId).CountAsync();
        }

        public async Task<List<string>> GetUserSubscribesById(string followerId)
        {
            return await _dbSet.Where(x => x.FollowerId == followerId).Select(x => x.FollowedUserId).ToListAsync();
        }

        public async Task<bool> IsUserAndChanelExist(string followerId, string followedId)
        {
            var follower = await _userManager.FindByIdAsync(followerId);
            var followed = await _userManager.FindByIdAsync(followedId);
            return follower != null && followed != null;
        }

        public async Task<bool> IsUserSubscribedToChanel(string followerId, string followedId)
        {
            return await _dbSet.AnyAsync(x => x.FollowerId == followerId && x.FollowedUserId == followedId);
        }

        public async Task RemoveUserSubscribeFromChanel(string followerId, string followedId)
        {
            if (await IsUserAndChanelExist(followerId, followedId))
            {
                var subscribe = await GetByIdAsync(followerId, followedId);
                if (subscribe != null)
                {
                    await Delete(subscribe);
                }
            }
        }

        public async Task SubscribeUserToChanel(string followerId, string followedId)
        {
            if (await IsUserAndChanelExist(followerId, followedId))
            {
                var subscription = new Subscription
                {
                    FollowerId = followerId,
                    FollowedUserId = followedId,
                    FollowedAt = DateTime.UtcNow
                };
                await Add(subscription);
            }
        }
    }
}