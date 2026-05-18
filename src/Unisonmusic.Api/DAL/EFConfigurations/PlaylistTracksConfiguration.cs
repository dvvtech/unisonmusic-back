using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Unisonmusic.Api.DAL.Entities;

namespace Unisonmusic.Api.DAL.EFConfigurations
{
    public class PlaylistTracksConfiguration : IEntityTypeConfiguration<PlaylistTrackEntity>
    {
        public void Configure(EntityTypeBuilder<PlaylistTrackEntity> builder)
        {
            builder.ToTable("playlist_tracks");

            builder.HasKey(x => new
            {
                x.PlaylistId,
                x.TrackId
            });

            builder.Property(x => x.PlaylistId)
                .IsRequired();

            builder.Property(x => x.TrackId)
                .IsRequired();

            builder.Property(x => x.AddedAtUtc)
                .IsRequired();

            builder.HasIndex(x => x.TrackId);

            builder.HasOne(x => x.Playlist)
                .WithMany(x => x.PlaylistTracks)
                .HasForeignKey(x => x.PlaylistId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Track)
                .WithMany(x => x.PlaylistTracks)
                .HasForeignKey(x => x.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
