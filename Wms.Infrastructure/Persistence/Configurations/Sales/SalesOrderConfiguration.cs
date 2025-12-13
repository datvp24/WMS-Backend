using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.Sales;

namespace Wms.Infrastructure.Persistence.Configurations.Sales
{
    public class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
    {
        public void Configure(EntityTypeBuilder<SalesOrder> builder)
        {
            builder.ToTable("SalesOrders");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Code).IsRequired().HasMaxLength(50);
            builder.HasIndex(x => x.Code).IsUnique();

            builder.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("DRAFT");
            builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            builder.Property(x => x.LockedStock).HasDefaultValue(false);

            builder.HasMany(x => x.Items)
                   .WithOne(x => x.SalesOrder)
                   .HasForeignKey(x => x.SalesOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.GoodsIssues)
                   .WithOne(x => x.SalesOrder)
                   .HasForeignKey(x => x.SalesOrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Customer)
                   .WithMany()
                   .HasForeignKey(x => x.CustomerId)
                   .OnDelete(DeleteBehavior.Restrict);


        }
    }
}
