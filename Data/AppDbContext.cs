using Microsoft.EntityFrameworkCore;
using VideoProjektAspApi.Model;

namespace VideoProjektAspApi.Data
{
    public class AppDbContext: DbContext
    {
        private readonly string conn = "Server=localhost;Database=videodb;Uid=root;Pwd=;";

        public DbSet<Video> Videos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql(conn, ServerVersion.AutoDetect(conn));
        }
    }
}
