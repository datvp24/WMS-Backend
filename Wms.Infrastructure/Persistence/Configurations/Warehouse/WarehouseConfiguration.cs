using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Infrastructure.Persistence.Configurations.WarehouseConfig
{
    public class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
    {
        public void Configure(EntityTypeBuilder<Warehouse> builder)
        {
            builder.ToTable("Warehouses");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedNever();

            builder.Property(x => x.Code)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Address)
                .HasMaxLength(500);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.HasIndex(x => x.Code).IsUnique();

            builder.HasMany(x => x.Locations)
                .WithOne(x => x.Warehouse)
                .HasForeignKey(x => x.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
