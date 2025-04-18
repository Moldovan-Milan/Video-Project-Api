using Microsoft.AspNetCore.Identity;
using OmegaStreamServices.Models;
using OmegaStreamServices.Services.Base;
using OmegaStreamServices.Services.Repositories;

public class SubscriptionRepository : BaseRepository<Subscription>, ISubscriptionRepository
{
    private readonly IGenericRepository _genericRepo;
    private readonly UserManager<User> _userManager;

    public SubscriptionRepository(IGenericRepository genericRepo, UserManager<User> userManager)
    {
        _genericRepo = genericRepo;
        _userManager = userManager;
    }

    public async Task<bool> IsUserAndChanelExist(string followerId, string followedId)
    {
        var follower = await _userManager.FindByIdAsync(followerId);
        var followed = await _userManager.FindByIdAsync(followedId);
        return follower != null && followed != null;
    }

    public async Task RemoveUserSubscribeFromChanel(string followerId, string followedId)
    {
        if (await IsUserAndChanelExist(followerId, followedId))
        {
            var subscription = await _genericRepo.FirstOrDefaultAsync<Subscription>(
                x => x.FollowerId == followerId && x.FollowedUserId == followedId
            );
            if (subscription != null)
            {
                await _genericRepo.DeleteAsync(subscription);
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
            await _genericRepo.AddAsync(subscription);
        }
    }
}
