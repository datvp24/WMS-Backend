using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Inventorys;

namespace Wms.Infrastructure.Persistence.Configurations
{
    public class LotConfiguration : IEntityTypeConfiguration<Lot>
    {
        public void Configure(EntityTypeBuilder<Lot> builder)
        {
            builder.ToTable("Lots");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                   .IsRequired();

            builder.Property(x => x.productId)
                   .IsRequired();

            builder.Property(x => x.Code)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.ManufacturingDate);

            builder.Property(x => x.ExpiryDate);

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.HasIndex(x => new { x.productId, x.Code })
                   .IsUnique()
                   .HasDatabaseName("UX_Lot_Product_LotCode");

            // Index phục vụ FEFO
            builder.HasIndex(x => new { x.productId, x.ExpiryDate })
                   .HasDatabaseName("IX_Lot_Product_ExpiryDate");
        }
    }
}
