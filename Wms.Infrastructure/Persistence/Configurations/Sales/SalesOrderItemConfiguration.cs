using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Sales;

namespace Wms.Infrastructure.Persistence.Configurations.Sales
{
    public class SalesOrderItemConfiguration : IEntityTypeConfiguration<SalesOrderItem>
    {
        public void Configure(EntityTypeBuilder<SalesOrderItem> builder)
        {
            builder.ToTable("SalesOrderItems");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            builder.Property(x => x.TotalPrice).HasColumnType("decimal(18,2)");

            builder.HasOne(x => x.SalesOrder)
                   .WithMany(x => x.Items)
                   .HasForeignKey(x => x.SalesOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Product)
                   .WithMany()
                   .HasForeignKey(x => x.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
