using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Unisonmusic.Api.DAL.Entities;

namespace Unisonmusic.Api.DAL.EFConfigurations
{
    public class UserTracksConfiguration : IEntityTypeConfiguration<UserTrackEntity>
    {
        public void Configure(EntityTypeBuilder<UserTrackEntity> builder)
        {
            builder.ToTable("user_tracks");

            builder.HasKey(x => new
            {
                x.UserId,
                x.TrackId
            });

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.TrackId)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasIndex(x => x.TrackId);

            builder.HasOne(x => x.Track)
                .WithMany(x => x.UserTracks)
                .HasForeignKey(x => x.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
