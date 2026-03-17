using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Warehouses;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .ValueGeneratedNever();

        builder.Property(x => x.Code)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.Description)
               .HasMaxLength(250);

        builder.HasIndex(x => new { x.WarehouseId, x.Code })
               .IsUnique()
               .HasDatabaseName("UX_Location_Warehouse_Code");

        builder.HasOne(x => x.Warehouse)
               .WithMany(x => x.Locations)
               .HasForeignKey(x => x.WarehouseId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
