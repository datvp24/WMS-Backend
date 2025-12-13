using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wms.Domain.Entity.MasterData;

namespace Wms.Infrastructure.Persistence.Configurations.MasterData
{
    public class BrandConfiguration : IEntityTypeConfiguration<Brand>
    {
        public void Configure(EntityTypeBuilder<Brand> builder)
        {
            builder.ToTable("Brands");

            // Khóa chính
            builder.HasKey(x => x.Id);

            // Cột Code
            builder.Property(x => x.Code)
                .HasColumnType("varchar(50)")
                .IsRequired();

            // Cột Name
            builder.Property(x => x.Name)
                .HasColumnType("varchar(200)")
                .IsRequired();

            // Cột Description (nullable)
            builder.Property(x => x.Description)
                .HasColumnType("longtext");

            // Cột IsActive
            builder.Property(x => x.IsActive)
                .HasColumnType("tinyint(1)")
                .IsRequired()
                .HasDefaultValue(true);

            // Cột CreatedAt
            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Index
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.Name).IsUnique();
        }
    }
}
