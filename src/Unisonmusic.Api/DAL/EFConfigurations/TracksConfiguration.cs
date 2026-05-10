using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Unisonmusic.Api.DAL.Entities;

namespace Unisonmusic.Api.DAL.EFConfigurations
{
    public class TracksConfiguration : IEntityTypeConfiguration<TrackEntity>
    {
        public void Configure(EntityTypeBuilder<TrackEntity> builder)
        {
            builder.ToTable("tracks");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(2048);

            builder.Property(x => x.S3ObjectKey)
                .IsRequired()
                .HasMaxLength(512);

            builder.HasIndex(x => x.Url)
                .IsUnique();
        }
    }
}
