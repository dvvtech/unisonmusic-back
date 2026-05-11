using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL.EFConfigurations;
using Unisonmusic.Api.DAL.Entities;

namespace Unisonmusic.Api.DAL
{
    public class UnisonmusicDbContext : DbContext
    {
        public DbSet<TrackEntity> Tracks { get; set; }

        public DbSet<UserTrackEntity> UserTracks { get; set; }

        public UnisonmusicDbContext(DbContextOptions<UnisonmusicDbContext> options)
            :base(options)
        {            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Регистрация конфигураций
            modelBuilder.ApplyConfiguration(new TracksConfiguration());
            modelBuilder.ApplyConfiguration(new UserTracksConfiguration());            

            base.OnModelCreating(modelBuilder);
        }
    }
}
