using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Domain.Entity.StockTakes;

namespace Wms.Infrastructure.Persistence.Configurations.Stocktake
{
    public class StockTakeConfiguration : IEntityTypeConfiguration<StockTake>
    {
        public void Configure(EntityTypeBuilder<StockTake> builder)
        {
            builder.ToTable("StockTakes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Code).IsRequired().HasMaxLength(50);

            // Quan hệ 1-n với Warehouse
            builder.HasOne(x => x.Warehouse)
                   .WithMany()
                   .HasForeignKey(x => x.WarehouseId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Quan hệ 1-n với Items
            builder.HasMany(x => x.Items)
                   .WithOne(x => x.StockTake)
                   .HasForeignKey(x => x.StockTakeId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
