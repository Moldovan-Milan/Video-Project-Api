using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Models;
using System.Reflection.Emit;

namespace OmegaStreamServices.Data
{
    public class AppDbContext : IdentityDbContext<User>
    {
        public DbSet<Video> Videos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<VideoLikes> VideoLikes { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserChats> UserChats { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }


        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Identity
            builder.Entity<IdentityUserLogin<string>>().HasKey(login => new { login.LoginProvider, login.ProviderKey });
            builder.Entity<IdentityUserRole<string>>().HasKey(role => new { role.UserId, role.RoleId });
            builder.Entity<IdentityUserToken<string>>().HasKey(token => new { token.UserId, token.LoginProvider, token.Name });

            builder.Entity<Subscription>()
               .HasOne(s => s.Follower)
               .WithMany(u => u.Following)
               .HasForeignKey(s => s.FollowerId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Subscription>()
                .HasOne(s => s.FollowedUser)
                .WithMany(u => u.Followers)
                .HasForeignKey(s => s.FollowedUserId)
                .OnDelete(DeleteBehavior.Restrict);


            #region VideoLikes table
            builder.Entity<VideoLikes>()
                    .HasKey(vl => new { vl.UserId, vl.VideoId });
            builder.Entity<VideoLikes>()
                .HasOne(vl => vl.User)
                .WithMany(v => v.VideoLikes)
                .HasForeignKey(v => v.UserId);

            builder.Entity<VideoLikes>()
                .HasOne(vl => vl.Video)
                .WithMany(v => v.VideoLikes)
                .HasForeignKey(v => v.VideoId);

            #endregion VideoLikes table
        }
    }
}
