using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Data
{
    public class AppDbContext: IdentityDbContext<User>
    {
        

        public DbSet<Video> Videos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Image> Images { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    }
}
