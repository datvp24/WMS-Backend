using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Transfer;

namespace Wms.Infrastructure.Persistence.Configurations.Transfer
{
    public class TransferOrderConfiguration : IEntityTypeConfiguration<TransferOrder>
    {
        public void Configure(EntityTypeBuilder<TransferOrder> builder)
        {
            builder.ToTable("transfer_orders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .HasColumnType("char(36)");

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.HasIndex(x => x.Code)
                   .IsUnique();

            builder.Property(x => x.Status)
                   .HasConversion<int>()
                   .IsRequired();

            // From Warehouse
            builder.HasOne(x => x.FromWarehouse)
                   .WithMany()
                   .HasForeignKey(x => x.FromWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            // To Warehouse
            builder.HasOne(x => x.ToWarehouse)
                   .WithMany()
                   .HasForeignKey(x => x.ToWarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.CreatedAt)
                   .HasColumnType("datetime");

            builder.Property(x => x.ApprovedAt)
                   .HasColumnType("datetime");

            builder.HasIndex(x => x.Status);
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
