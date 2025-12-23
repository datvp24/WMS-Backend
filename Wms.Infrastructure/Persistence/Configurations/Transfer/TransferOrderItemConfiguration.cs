using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Transfer;

namespace Wms.Infrastructure.Persistence.Configurations.Transfer
{
    public class TransferOrderItemConfiguration : IEntityTypeConfiguration<TransferOrderItem>
    {
        public void Configure(EntityTypeBuilder<TransferOrderItem> builder)
        {
            builder.ToTable("transfer_order_items");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnType("char(36)");

            builder.Property(x => x.TransferOrderId)
                   .HasColumnType("char(36)");

            builder.Property(x => x.FromLocationId)
                   .HasColumnType("char(36)");

            builder.Property(x => x.ToLocationId)
                   .HasColumnType("char(36)");

            builder.Property(x => x.Quantity)
                   .HasPrecision(18, 2)
                   .IsRequired();

            builder.HasOne(x => x.FromLocation)
                   .WithMany()
                   .HasForeignKey(x => x.FromLocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ToLocation)
                   .WithMany()
                   .HasForeignKey(x => x.ToLocationId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new
            {
                x.TransferOrderId,
                x.ProductId,
                x.FromLocationId,
                x.ToLocationId
            })
            .HasDatabaseName("idx_transfer_item_lookup");
        }
    }
}
