using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Purchase;

namespace Wms.Infrastructure.Persistence.Configurations.Purchase
{
    public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
        {
            builder.ToTable("PurchaseOrders");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.Status)
                   .IsRequired()
                   .HasMaxLength(20);

            builder.Property(x => x.SupplierId)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.PurchaseOrder)
                   .HasForeignKey(x => x.PurchaseOrderId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PurchaseOrderItemConfiguration : IEntityTypeConfiguration<PurchaseOrderItem>
    {
        public void Configure(EntityTypeBuilder<PurchaseOrderItem> builder)
        {
            builder.ToTable("PurchaseOrderItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .IsRequired();

            builder.Property(x => x.Price)
                   .HasColumnType("decimal(18,2)")
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();
        }
    }

    public class GoodsReceiptConfiguration : IEntityTypeConfiguration<GoodsReceipt>
    {
        public void Configure(EntityTypeBuilder<GoodsReceipt> builder)
        {
            builder.ToTable("GoodsReceipts");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(50);

            builder.Property(x => x.PurchaseOrderId)
                   .IsRequired();

            builder.Property(x => x.WarehouseId)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.GoodsReceipt)
                   .HasForeignKey(x => x.GoodsReceiptId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.PurchaseOrder)
        .WithMany(x => x.GoodsReceipts) // ✅ đúng kiểu
        .HasForeignKey(x => x.PurchaseOrderId)
        .OnDelete(DeleteBehavior.Restrict);

        }
    }

    public class GoodsReceiptItemConfiguration : IEntityTypeConfiguration<GoodsReceiptItem>
    {
        public void Configure(EntityTypeBuilder<GoodsReceiptItem> builder)
        {
            builder.ToTable("GoodsReceiptItems");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ProductId)
                   .IsRequired();

            builder.Property(x => x.Quantity)
                   .IsRequired();


            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.UpdatedAt)
                   .IsRequired();
        }
    }
}
