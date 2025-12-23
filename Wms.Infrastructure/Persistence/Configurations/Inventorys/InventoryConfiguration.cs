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

        // Primary key
        builder.HasKey(x => x.Id);

        // Quantities
        builder.Property(x => x.OnHandQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.LockedQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired();

        builder.Property(x => x.InTransitQuantity)
               .HasColumnType("decimal(18,4)")
               .IsRequired()
               .HasDefaultValue(0m);

        // Timestamps
        builder.Property(x => x.CreatedAt)
               .HasColumnType("datetime")
               .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(x => x.UpdatedAt)
               .HasColumnType("datetime")
               .IsRequired(false);

        // Unique constraint: mỗi Product + Warehouse + Location chỉ có 1 record
        builder.HasIndex(x => new { x.WarehouseId, x.LocationId, x.ProductId })
               .IsUnique();

        // Index để query nhanh
        builder.HasIndex(x => x.ProductId);
        builder.HasIndex(x => x.WarehouseId);
        builder.HasIndex(x => x.LocationId);

        // Quan hệ
        builder.HasOne<Warehouse>()
               .WithMany()
               .HasForeignKey(x => x.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
               .WithMany()
               .HasForeignKey(x => x.LocationId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Product>()
               .WithMany()
               .HasForeignKey(x => x.ProductId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
