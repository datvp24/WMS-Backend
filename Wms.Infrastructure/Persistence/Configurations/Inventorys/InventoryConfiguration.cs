using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;

public class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.LockedQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.CreatedAt)
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
               .HasColumnType("datetime")
               .IsRequired(false)
               .HasDefaultValueSql("CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP");

        // Unique constraint
        builder.HasIndex(x => new { x.WarehouseId, x.LocationId, x.ProductId })
               .IsUnique();

        // Index để query nhanh
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.LocationId);

        // Quan hệ với các bảng liên quan
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId);
        builder.HasOne<Location>().WithMany().HasForeignKey(x => x.LocationId);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId);
    }
}
