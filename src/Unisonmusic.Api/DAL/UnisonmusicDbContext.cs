using Microsoft.EntityFrameworkCore;
using Unisonmusic.Api.DAL.EFConfigurations;
using Unisonmusic.Api.DAL.Entities;

namespace Unisonmusic.Api.DAL
{
    public class UnisonmusicDbContext : DbContext
    {
        public DbSet<TrackEntity> Tracks { get; set; }

        public DbSet<UserTrackEntity> UserTracks { get; set; }

        public DbSet<PlaylistEntity> Playlists { get; set; }

        public DbSet<PlaylistTrackEntity> PlaylistTracks { get; set; }

        public DbSet<SubscriptionEntity> Subscriptions { get; set; }

        public UnisonmusicDbContext(DbContextOptions<UnisonmusicDbContext> options)
            :base(options)
        {            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TracksConfiguration());
            modelBuilder.ApplyConfiguration(new UserTracksConfiguration());
            modelBuilder.ApplyConfiguration(new PlaylistsConfiguration());
            modelBuilder.ApplyConfiguration(new PlaylistTracksConfiguration());
            modelBuilder.ApplyConfiguration(new SubscriptionsConfiguration());

            base.OnModelCreating(modelBuilder);
        }
    }
}
