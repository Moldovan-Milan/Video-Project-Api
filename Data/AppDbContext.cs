using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Data
{
    public class AppDbContext: IdentityDbContext<User>
    {
        private readonly string conn = "Server=localhost;Database=videodb;Uid=root;Pwd=;";

        public DbSet<Video> Videos { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Image> Images { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(conn, ServerVersion.AutoDetect(conn));
        }
    }
}
