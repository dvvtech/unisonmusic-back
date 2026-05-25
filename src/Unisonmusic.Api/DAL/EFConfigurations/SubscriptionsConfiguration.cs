using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Unisonmusic.Api.DAL.Entities;

namespace Unisonmusic.Api.DAL.EFConfigurations;

public class SubscriptionsConfiguration : IEntityTypeConfiguration<SubscriptionEntity>
{
    public void Configure(EntityTypeBuilder<SubscriptionEntity> builder)
    {
        builder.ToTable("subscriptions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedOnAdd();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.Plan)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.YooKassaPaymentId)
            .HasMaxLength(256);

        builder.Property(x => x.ActivatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();
    }
}
