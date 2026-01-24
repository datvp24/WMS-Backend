using Microsoft.EntityFrameworkCore;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Infrastructure.Seed;

public static class TechnicalPlasticWarehouseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var date = new DateTime(2026, 1, 3);
        var random = new Random(2026);

        // ================= UNITS (200) =================
        if (!await db.Units.AnyAsync())
        {
            var unitTypes = new[]
            {
                ("KG", "Kilogram"), ("TON", "Tấn"), ("G", "Gram"),
                ("M", "Mét"), ("M2", "Mét vuông"), ("M3", "Mét khối"),
                ("SHEET", "Tấm"), ("ROLL", "Cuộn"), ("BAG", "Bao"),
                ("PALLET", "Pallet"), ("BUNDLE", "Bó"), ("BOX", "Thùng"),
                ("PIECE", "Cái"), ("SET", "Bộ")
            };

            var units = Enumerable.Range(1, 200).Select(i =>
            {
                var u = unitTypes[(i - 1) % unitTypes.Length];
                return new Unit
                {
                    Code = $"{u.Item1}_{i:D3}",
                    Name = $"{u.Item2} {i}",
                    IsActive = random.Next(100) > 5, // 95% active
                    CreatedAt = date.AddDays(-random.Next(1, 730))
                };
            }).ToList();

            await db.Units.AddRangeAsync(units);
            await db.SaveChangesAsync();
        }

        // ================= BRANDS (200) =================
        if (!await db.Brands.AnyAsync())
        {
            var brandNames = new[]
            {
                "DuPont", "BASF", "Sabic", "Covestro", "Mitsubishi Chemical",
                "Evonik", "DSM", "Solvay", "Celanese", "Toray",
                "LG Chem", "Formosa Plastics", "Chi Mei", "Samsung SDI", "SK Chemicals",
                "Teijin", "Asahi Kasei", "Daicel", "Kaneka", "Kuraray",
                "Arkema", "Borealis", "Ineos", "LyondellBasell", "Braskem"
            };

            var brands = Enumerable.Range(1, 200).Select(i => new Brand
            {
                Code = $"BR_{i:D4}",
                Name = i <= brandNames.Length
                    ? brandNames[i - 1]
                    : $"{brandNames[i % brandNames.Length]} {i / brandNames.Length + 1}",
                IsActive = true,
                CreatedAt = date.AddDays(-random.Next(1, 1095))
            }).ToList();

            await db.Brands.AddRangeAsync(brands);
            await db.SaveChangesAsync();
        }

        // ================= CATEGORIES (200) =================
        if (!await db.Categories.AnyAsync())
        {
            var categoryGroups = new[]
            {
                // Engineering Plastics
                "ABS", "PC", "PA6", "PA66", "POM", "PMMA", "PBT", "PET",
                "PSU", "PPS", "PEEK", "PEI", "LCP", "PAI", "PI",
                // Commodity Plastics
                "PP", "PE-HD", "PE-LD", "PS", "PVC", "EVA",
                // Specialty Plastics
                "TPU", "TPE", "TPV", "PTFE", "FEP", "PFA",
                // Composite Materials
                "PC+ABS", "PC+PBT", "PA+GF", "PP+GF", "Nylon GF",
                // Recycled Materials
                "R-ABS", "R-PC", "R-PP", "R-PE", "R-PET"
            };

            var categories = Enumerable.Range(1, 200).Select(i =>
            {
                var catType = categoryGroups[(i - 1) % categoryGroups.Length];
                var suffix = i > categoryGroups.Length ? $" Grade {(i - 1) / categoryGroups.Length + 1}" : "";

                return new Category
                {
                    Code = $"CAT_{i:D4}",
                    Name = $"{catType}{suffix}",
                    IsActive = true,
                    CreatedAt = date.AddDays(-random.Next(1, 900))
                };
            }).ToList();

            await db.Categories.AddRangeAsync(categories);
            await db.SaveChangesAsync();
        }

        // ================= SUPPLIERS (200) =================
        if (!await db.Suppliers.AnyAsync())
        {
            var supplierPrefixes = new[]
            {
                "Việt Nam Plastic", "Sài Gòn Polymer", "Hà Nội Engineering",
                "Global Resin", "Asia Pacific Material", "Euro Tech Plastic",
                "Japan Chemical", "Korea Polymer", "China Plastics",
                "Thai Engineering Material", "Singapore Resin", "Taiwan Polymer",
                "Delta Plastics", "Pacific Material", "Golden Resin",
                "Star Engineering", "Diamond Polymer", "Royal Plastics",
                "Premier Material", "Elite Engineering", "Supreme Resin"
            };

            var districts = new[]
            {
                "Quận 1, TP.HCM", "Quận Bình Thạnh, TP.HCM", "Quận 7, TP.HCM",
                "Thủ Đức, TP.HCM", "Bình Dương", "Đồng Nai", "Long An",
                "Ba Đình, Hà Nội", "Cầu Giấy, Hà Nội", "Hai Bà Trưng, Hà Nội",
                "Hải Châu, Đà Nẵng", "Ngũ Hành Sơn, Đà Nẵng"
            };

            var suppliers = Enumerable.Range(1, 200).Select(i => new Supplier
            {
                Code = $"SUP_{i:D4}",
                Name = i <= supplierPrefixes.Length
                    ? $"{supplierPrefixes[i - 1]} Co., Ltd"
                    : $"{supplierPrefixes[i % supplierPrefixes.Length]} {i / supplierPrefixes.Length + 1} Co., Ltd",
                Email = $"sales{i}@supplier{i}.com",
                Phone = $"(+84) {random.Next(20, 99)}{random.Next(1000000, 9999999)}",
                Address = $"{random.Next(1, 999)} Đường {random.Next(1, 50)}, {districts[i % districts.Length]}",
                IsActive = random.Next(100) > 3, // 97% active
                CreatedAt = date.AddDays(-random.Next(1, 1200))
            }).ToList();

            await db.Suppliers.AddRangeAsync(suppliers);
            await db.SaveChangesAsync();
        }

        // ================= WAREHOUSES (200) =================
        if (!await db.Warehouses.AnyAsync())
        {
            var warehouseAreas = new[]
            {
                ("Bình Dương", "KCN Việt Nam Singapore"),
                ("Đồng Nai", "KCN Long Thành"),
                ("Long An", "KCN Tân Đô"),
                ("Bà Rịa-Vũng Tàu", "KCN Phú Mỹ"),
                ("TP.HCM", "KCN Tân Bình"),
                ("Hà Nội", "KCN Thăng Long"),
                ("Hải Phòng", "KCN VSIP Hải Phòng"),
                ("Đà Nẵng", "KCN Hòa Khánh"),
                ("Bắc Ninh", "KCN Yên Phong"),
                ("Hưng Yên", "KCN Phố Nối"),
                ("Hải Dương", "KCN Nam Sách"),
                ("Quảng Ninh", "KCN Cái Lân")
            };

            var warehouses = Enumerable.Range(1, 200).Select(i =>
            {
                var area = warehouseAreas[(i - 1) % warehouseAreas.Length];
                return new Warehouse
                {
                    Id = Guid.NewGuid(),
                    Code = $"WH_{i:D3}",
                    Name = $"Kho {area.Item1} {(i - 1) / warehouseAreas.Length + 1}",
                    Address = $"Lô {random.Next(1, 50)}, {area.Item2}, {area.Item1}",
                    WarehouseType = (WarehouseType)(i % 4),
                    Status = random.Next(100) > 10 ? WarehouseStatus.Active : WarehouseStatus.Inactive,
                    CreatedAt = date.AddDays(-random.Next(1, 1000))
                };
            }).ToList();

            await db.Warehouses.AddRangeAsync(warehouses);
            await db.SaveChangesAsync();
        }

        var warehouseIds = await db.Warehouses.Select(x => x.Id).ToListAsync();

        // ================= LOCATIONS (200) =================
        if (!await db.Locations.AnyAsync())
        {
            var locationTypes = new[] { "Kệ lưu trữ", "Khu vực tập kết", "Khu vực chờ" };

            var locations = Enumerable.Range(1, 200).Select(i =>
            {
                var zone = (char)('A' + (i - 1) / 25);
                var row = ((i - 1) % 25) / 5 + 1;
                var bay = (i - 1) % 5 + 1;
                var level = random.Next(1, 6);
                var locType = (LocationType)(i % 3);

                return new Location
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseIds[i % warehouseIds.Count],
                    Code = $"{zone}{row:D2}-{bay:D2}-{level:D2}",
                    Type = locType,
                    Description = $"{locationTypes[(int)locType]} - Zone {zone}, Hàng {row}, Cột {bay}, Tầng {level}",
                    IsActive = random.Next(100) > 8, // 92% active
                    CreatedAt = date.AddDays(-random.Next(1, 800))
                };
            }).ToList();

            await db.Locations.AddRangeAsync(locations);
            await db.SaveChangesAsync();
        }

        var locationIds = await db.Locations.Select(x => x.Id).ToListAsync();

        // ================= PRODUCTS (200) =================
        if (!await db.Products.AnyAsync())
        {
            var unitIds = await db.Units.Select(x => x.Id).ToListAsync();
            var brandIds = await db.Brands.Select(x => x.Id).ToListAsync();
            var categoryIds = await db.Categories.Select(x => x.Id).ToListAsync();
            var supplierIds = await db.Suppliers.Select(x => x.Id).ToListAsync();

            var plasticTypes = new[]
            {
                "ABS Natural", "ABS Black", "PC Clear", "PC Smoke",
                "PA6 Natural", "PA66 Black", "POM White", "POM Black",
                "PMMA Clear", "PBT Black", "PET Natural", "PP Natural",
                "PE-HD Natural", "PS Crystal", "PVC Transparent",
                "TPU 95A", "TPE Shore 60", "PEEK Natural",
                "PC+ABS Black", "PA6+GF30", "PP+GF20"
            };

            var products = Enumerable.Range(1, 200).Select(i =>
            {
                var plasticType = plasticTypes[(i - 1) % plasticTypes.Length];
                var grade = $"Grade {random.Next(100, 999)}";
                var mfi = random.Next(5, 50);

                return new Product
                {
                    Code = $"PROD_{i:D4}",
                    Name = i > plasticTypes.Length
                        ? $"{plasticType} {grade} MFI{mfi}"
                        : $"{plasticType} {grade}",
                    Type = (ProductType)(i % 2),
                    UnitId = unitIds[i % unitIds.Count],
                    BrandId = brandIds[i % brandIds.Count],
                    CategoryId = categoryIds[i % categoryIds.Count],
                    SupplierId = supplierIds[i % supplierIds.Count],
                    IsActive = random.Next(100) > 5, // 95% active
                    CreatedAt = date.AddDays(-random.Next(1, 600))
                };
            }).ToList();

            await db.Products.AddRangeAsync(products);
            await db.SaveChangesAsync();
        }

        // ================= CUSTOMERS (200) =================
        if (!await db.Customers.AnyAsync())
        {
            var customerPrefixes = new[]
            {
        "Molding Precision", "Auto Part Tech", "Electronic Component",
        "Plastic Solution", "Smart Design", "Industrial Manufacturing",
        "Home Appliance", "Medical Device", "Packaging Expert",
        "Alpha Polymer", "Sigma Engineering", "Omega Tech",
        "Vina Molding", "Sài Gòn Injection", "Hà Nội Tooling",
        "Mekong Plastic", "Red River Tech", "Pacific Manufacturing"
    };

            var districts = new[]
            {
        "Quận 9, TP.HCM", "Quận 12, TP.HCM", "KCN Tân Tạo, TP.HCM",
        "KCN Amata, Đồng Nai", "KCN VSIP, Bình Dương", "KCN Quế Võ, Bắc Ninh",
        "KCN Thăng Long, Hà Nội", "KCN Quang Minh, Hà Nội", "Hải Phòng",
        "Vĩnh Phúc", "Long An", "Bắc Giang"
    };

            var customers = Enumerable.Range(1, 200).Select(i => new Customer
            {
                Code = $"CUS_{i:D4}",
                Name = i <= customerPrefixes.Length
                    ? $"{customerPrefixes[i - 1]} JSC"
                    : $"{customerPrefixes[i % customerPrefixes.Length]} No.{i / customerPrefixes.Length + 1} Corp",
                Email = $"contact{i}@customer{i}.vn",
                Phone = $"(+84) {random.Next(20, 99)}{random.Next(1000000, 9999999)}",
                Address = $"{random.Next(10, 500)} Đường số {random.Next(1, 100)}, {districts[i % districts.Length]}",
                IsActive = true,
                CreatedAt = date.AddDays(-random.Next(1, 1500))
            }).ToList();

            await db.Customers.AddRangeAsync(customers);
            await db.SaveChangesAsync();
        }
        // ================= INVENTORIES (200) =================
        if (!await db.Inventories.AnyAsync())
        {
            var productIds = await db.Products.Select(x => x.Id).ToListAsync();

            var inventories = Enumerable.Range(1, 200).Select(i =>
            {
                var onHand = random.Next(50, 10000);
                var locked = random.Next(0, (int)(onHand * 0.2));
                var inTransit = random.Next(0, (int)(onHand * 0.3));

                return new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseIds[i % warehouseIds.Count],
                    LocationId = locationIds[i % locationIds.Count],
                    ProductId = productIds[i % productIds.Count],
                    OnHandQuantity = onHand,
                    LockedQuantity = locked,
                    InTransitQuantity = inTransit,
                    CreatedAt = date.AddDays(-random.Next(1, 500))
                };
            }).ToList();

            await db.Inventories.AddRangeAsync(inventories);
            await db.SaveChangesAsync();
        }

        Console.WriteLine("✅ SEED DATA HOÀN TẤT - 200 DÒNG/BẢNG - KHO NHỰA KỸ THUẬT");
        Console.WriteLine($"   📦 Units: 200");
        Console.WriteLine($"   🏷️  Brands: 200");
        Console.WriteLine($"   📂 Categories: 200");
        Console.WriteLine($"   🏢 Suppliers: 200");
        Console.WriteLine($"   🏭 Warehouses: 200");
        Console.WriteLine($"   📍 Locations: 200");
        Console.WriteLine($"   🔧 Products: 200");
        Console.WriteLine($"   📊 Inventories: 200");
    }
}