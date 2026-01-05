using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Infrastructure.Seed;

public static class OtherSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        // Chỉ seed nếu các bảng chính chưa có dữ liệu (tránh seed lặp)
        if (await db.Units.AnyAsync()) return;

        var date = new DateTime(2026, 1, 3);

        // GUID cố định để dễ quản lý và debug
        var whHn = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var whHcm = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var whDn = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var locA01 = Guid.Parse("a0000000-0000-0000-0000-000000000001");
        var locA02 = Guid.Parse("a0000000-0000-0000-0000-000000000002");
        var locB01 = Guid.Parse("a0000000-0000-0000-0000-000000000003");
        var locX01 = Guid.Parse("a0000000-0000-0000-0000-000000000004");
        var locY01 = Guid.Parse("a0000000-0000-0000-0000-000000000005");

        // 1. Units
        var units = new[]
        {
            new Unit { Id = 1, Code = "UNIT", Name = "Cái", IsActive = true, CreatedAt = date },
            new Unit { Id = 2, Code = "BOX", Name = "Thùng", IsActive = true, CreatedAt = date },
            new Unit { Id = 3, Code = "PACK", Name = "Gói", IsActive = true, CreatedAt = date },
            new Unit { Id = 4, Code = "KG", Name = "Kilogram", IsActive = true, CreatedAt = date },
            new Unit { Id = 5, Code = "METER", Name = "Mét", IsActive = true, CreatedAt = date }
        };
        db.Units.AddRange(units);

        // 2. Brands
        var brands = new[]
        {
            new Brand { Id = 1, Code = "BR001", Name = "Samsung", IsActive = true, Description = "Thương hiệu điện tử Hàn Quốc", CreatedAt = date },
            new Brand { Id = 2, Code = "BR002", Name = "Apple", IsActive = true, Description = "Thương hiệu công nghệ Mỹ", CreatedAt = date },
            new Brand { Id = 3, Code = "BR003", Name = "Sony", IsActive = true, Description = "Thương hiệu Nhật Bản", CreatedAt = date },
            new Brand { Id = 4, Code = "BR004", Name = "Xiaomi", IsActive = true, Description = "Thương hiệu Trung Quốc", CreatedAt = date },
            new Brand { Id = 5, Code = "BR005", Name = "Adidas", IsActive = true, Description = "Thương hiệu thể thao", CreatedAt = date }
        };
        db.Brands.AddRange(brands);

        // 3. Categories
        var categories = new[]
        {
            new Category { Id = 1, Code = "CAT001", Name = "Điện thoại di động", IsActive = true, CreatedAt = date },
            new Category { Id = 2, Code = "CAT002", Name = "Laptop", IsActive = true, CreatedAt = date },
            new Category { Id = 3, Code = "CAT003", Name = "Tai nghe", IsActive = true, CreatedAt = date },
            new Category { Id = 4, Code = "CAT004", Name = "Giày thể thao", IsActive = true, CreatedAt = date },
            new Category { Id = 5, Code = "CAT005", Name = "Phụ kiện", IsActive = true, CreatedAt = date }
        };
        db.Categories.AddRange(categories);

        // 4. Suppliers
        var suppliers = new[]
        {
            new Supplier { Id = 1, Code = "SUP001", Name = "Công ty TNHH Samsung Việt Nam", Email = "samsung@supplier.com", Phone = "0123456789", Address = "KCN Yên Phong, Bắc Ninh", IsActive = true, CreatedAt = date },
            new Supplier { Id = 2, Code = "SUP002", Name = "Apple Việt Nam", Email = "apple@supplier.com", Phone = "0987654321", Address = "Hà Nội", IsActive = true, CreatedAt = date },
            new Supplier { Id = 3, Code = "SUP003", Name = "Công ty Sony Việt Nam", Email = "sony@supplier.com", Phone = "0912345678", Address = "TP.HCM", IsActive = true, CreatedAt = date },
            new Supplier { Id = 4, Code = "SUP004", Name = "Xiaomi Việt Nam", Email = "xiaomi@supplier.com", Phone = "0934567890", Address = "Hà Nội", IsActive = true, CreatedAt = date },
            new Supplier { Id = 5, Code = "SUP005", Name = "Adidas Việt Nam", Email = "adidas@supplier.com", Phone = "0901234567", Address = "TP.HCM", IsActive = true, CreatedAt = date }
        };
        db.Suppliers.AddRange(suppliers);

        // 5. Customers
        var customers = new[]
        {
            new Customer { Id = 1, Code = "CUS001", Name = "Nguyễn Văn A", Email = "nguyenvana@gmail.com", Phone = "0901001001", Address = "Hà Nội", IsActive = true, CreatedAt = date },
            new Customer { Id = 2, Code = "CUS002", Name = "Trần Thị B", Email = "tranthib@gmail.com", Phone = "0902002002", Address = "TP.HCM", IsActive = true, CreatedAt = date },
            new Customer { Id = 3, Code = "CUS003", Name = "Lê Văn C", Email = "levanc@gmail.com", Phone = "0903003003", Address = "Đà Nẵng", IsActive = true, CreatedAt = date },
            new Customer { Id = 4, Code = "CUS004", Name = "Công ty ABC", Email = "abc@company.com", Phone = "0281234567", Address = "Quận 1, TP.HCM", IsActive = true, CreatedAt = date },
            new Customer { Id = 5, Code = "CUS005", Name = "Công ty XYZ", Email = "xyz@company.com", Phone = "0249876543", Address = "Cầu Giấy, Hà Nội", IsActive = true, CreatedAt = date }
        };
        db.Customers.AddRange(customers);

        // 6. Warehouses
        db.Warehouses.AddRange(
            new Warehouse { Id = whHn, Code = "WH-HN", Name = "Kho Hà Nội", Address = "KCN Thăng Long, Hà Nội", Status = WarehouseStatus.Active, CreatedAt = date },
            new Warehouse { Id = whHcm, Code = "WH-HCM", Name = "Kho TP.HCM", Address = "KCN Tân Bình, TP.HCM", Status = WarehouseStatus.Active, CreatedAt = date },
            new Warehouse { Id = whDn, Code = "WH-DN", Name = "Kho Đà Nẵng", Address = "KCN Hòa Khánh, Đà Nẵng", Status = WarehouseStatus.Active, CreatedAt = date }
        );

        // 7. Locations
        db.Locations.AddRange(
            new Location { Id = locA01, WarehouseId = whHn, Code = "A01", Description = "Kệ A - Tầng 1", IsActive = true, CreatedAt = date },
            new Location { Id = locA02, WarehouseId = whHn, Code = "A02", Description = "Kệ A - Tầng 2", IsActive = true, CreatedAt = date },
            new Location { Id = locB01, WarehouseId = whHn, Code = "B01", Description = "Kệ B - Tầng 1", IsActive = true, CreatedAt = date },
            new Location { Id = locX01, WarehouseId = whHcm, Code = "X01", Description = "Kệ X - Khu vực lạnh", IsActive = true, CreatedAt = date },
            new Location { Id = locY01, WarehouseId = whHcm, Code = "Y01", Description = "Kệ Y - Khu vực thường", IsActive = true, CreatedAt = date }
        );

        await db.SaveChangesAsync(); // Lưu để có Id cho các entity tiếp theo

        // 8. Products
        var products = new[]
        {
            new Product { Id = 1, Code = "PROD001", Name = "iPhone 15 Pro", Description = "Điện thoại Apple mới nhất", IsActive = true, CategoryId = 1, UnitId = 1, BrandId = 2, SupplierId = 2, CreatedAt = date },
            new Product { Id = 2, Code = "PROD002", Name = "Galaxy S24 Ultra", Description = "Flagship Samsung", IsActive = true, CategoryId = 1, UnitId = 1, BrandId = 1, SupplierId = 1, CreatedAt = date },
            new Product { Id = 3, Code = "PROD003", Name = "MacBook Pro M3", Description = "Laptop Apple", IsActive = true, CategoryId = 2, UnitId = 1, BrandId = 2, SupplierId = 2, CreatedAt = date },
            new Product { Id = 4, Code = "PROD004", Name = "Xperia 1 V", Description = "Điện thoại Sony", IsActive = true, CategoryId = 1, UnitId = 1, BrandId = 3, SupplierId = 3, CreatedAt = date },
            new Product { Id = 5, Code = "PROD005", Name = "AirPods Pro 2", Description = "Tai nghe không dây Apple", IsActive = true, CategoryId = 3, UnitId = 1, BrandId = 2, SupplierId = 2, CreatedAt = date },
            new Product { Id = 6, Code = "PROD006", Name = "Ultraboost 23", Description = "Giày chạy Adidas", IsActive = true, CategoryId = 4, UnitId = 1, BrandId = 5, SupplierId = 5, CreatedAt = date }
        };
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        // 9. Inventories (tồn kho mẫu)
        db.Inventories.AddRange(
            new Inventory { Id = Guid.NewGuid(), WarehouseId = whHn, LocationId = locA01, ProductId = 1, OnHandQuantity = 50, LockedQuantity = 0, InTransitQuantity = 0, CreatedAt = date },
            new Inventory { Id = Guid.NewGuid(), WarehouseId = whHn, LocationId = locA02, ProductId = 2, OnHandQuantity = 30, LockedQuantity = 5, InTransitQuantity = 10, CreatedAt = date },
            new Inventory { Id = Guid.NewGuid(), WarehouseId = whHn, LocationId = locB01, ProductId = 3, OnHandQuantity = 20, LockedQuantity = 0, InTransitQuantity = 0, CreatedAt = date },
            new Inventory { Id = Guid.NewGuid(), WarehouseId = whHcm, LocationId = locX01, ProductId = 4, OnHandQuantity = 40, LockedQuantity = 0, InTransitQuantity = 0, CreatedAt = date },
            new Inventory { Id = Guid.NewGuid(), WarehouseId = whHcm, LocationId = locY01, ProductId = 5, OnHandQuantity = 100, LockedQuantity = 10, InTransitQuantity = 0, CreatedAt = date },
            new Inventory { Id = Guid.NewGuid(), WarehouseId = whHcm, LocationId = locY01, ProductId = 6, OnHandQuantity = 80, LockedQuantity = 0, InTransitQuantity = 20, CreatedAt = date }
        );

        await db.SaveChangesAsync();
    }
}