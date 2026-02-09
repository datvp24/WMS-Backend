using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        var baseDate = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        var random = new Random(2026);

        // Helper to generate random past dates
        DateTime RandomPastDate() => baseDate.AddDays(-random.Next(30, 730));

        // 1. UNITS (200)
        if (!await db.Units.AnyAsync())
        {
            var unitTemplates = new[] { ("KG", "Kilogram"), ("TON", "Tấn"), ("BAG", "Bao"), ("PAL", "Pallet"), ("PCS", "Cái") };
            var units = Enumerable.Range(1, 200).Select(i =>
            {
                var t = unitTemplates[(i - 1) % unitTemplates.Length];
                return new Unit
                {
                    Code = $"{t.Item1}_{i:D3}",
                    Name = $"{t.Item2} {i}",
                    IsActive = true,
                    CreatedAt = RandomPastDate()
                };
            }).ToList();

            db.Units.AddRange(units);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Units seeded (200)");
        }

        // 2. BRANDS (200)
        if (!await db.Brands.AnyAsync())
        {
            var brandRoots = new[] { "DuPont", "BASF", "Sabic", "Covestro", "LG Chem", "Teijin", "Toray" };
            var brands = Enumerable.Range(1, 200).Select(i => new Brand
            {
                Code = $"BR{i:D4}",
                Name = $"{brandRoots[i % brandRoots.Length]} Grade {i}",
                IsActive = true,
                Description = $"Nhà sản xuất nhựa kỹ thuật {i}",
                CreatedAt = RandomPastDate()
            }).ToList();

            db.Brands.AddRange(brands);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Brands seeded (200)");
        }

        // 3. CATEGORIES (200)
        if (!await db.Categories.AnyAsync())
        {
            var catRoots = new[] { "ABS", "PC", "PA66", "PP", "POM", "PBT", "TPU", "PEEK" };
            var categories = Enumerable.Range(1, 200).Select(i => new Category
            {
                Code = $"CAT{i:D4}",
                Name = $"{catRoots[i % catRoots.Length]} {i}",
                IsActive = true,
                CreatedAt = RandomPastDate()
            }).ToList();

            db.Categories.AddRange(categories);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Categories seeded (200)");
        }

        // 4. SUPPLIERS (200)
        if (!await db.Suppliers.AnyAsync())
        {
            var suppliers = Enumerable.Range(1, 200).Select(i => new Supplier
            {
                Code = $"SUP{i:D4}",
                Name = $"Công ty Nhựa Kỹ Thuật {i} Ltd",
                Email = $"contact{i}@nhuakythuat.vn",
                Phone = $"09{random.Next(10, 99)}{random.Next(1000000, 9999999)}",
                Address = $"KCN {i % 5 + 1}, Đồng Nai / Bình Dương",
                IsActive = true,
                CreatedAt = RandomPastDate()
            }).ToList();

            db.Suppliers.AddRange(suppliers);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Suppliers seeded (200)");
        }

        // 5. WAREHOUSES (200)
        if (!await db.Warehouses.AnyAsync())
        {
            var provinces = new[] { "Đồng Nai", "Bình Dương", "TP.HCM", "Long An", "Bà Rịa - Vũng Tàu" };
            var whs = Enumerable.Range(1, 200).Select(i => new Warehouse
            {
                Id = Guid.NewGuid(),
                Code = $"WH{i:D3}",
                Name = $"Kho Nhựa Kỹ Thuật {i}",
                Address = $"KCN {i % 8 + 1}, {provinces[i % provinces.Length]}",
                WarehouseType = (WarehouseType)(i % 4),           // 0→3: RawMaterial, FinishedGoods, Auxiliary, Chemical
                Status = WarehouseStatus.Active,
                CreatedAt = RandomPastDate(),
                UpdatedAt = null
            }).ToList();

            db.Warehouses.AddRange(whs);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Warehouses seeded (200)");
        }

        var warehouseIds = await db.Warehouses.Select(w => w.Id).ToListAsync();

        // 6. LOCATIONS (200)
        if (!await db.Locations.AnyAsync())
        {
            var locationZones = new[] { "A", "B", "C", "D", "E", "F" };
            var locs = Enumerable.Range(1, 200).Select(i => new Location
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseIds[(i - 1) % warehouseIds.Count],
                Code = $"LOC-{i:D4}",
                Description = $"Vị trí {i} - Khu {locationZones[i % locationZones.Length]}",
                Type = (LocationType)((i % 6) + 1),               // 1→6: Receiving, Storage, Shipping, Picking, Damage, Return
                IsActive = true,
                CreatedAt = RandomPastDate(),
                UpdatedAt = null
            }).ToList();

            db.Locations.AddRange(locs);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Locations seeded (200)");
        }

        var locationIds = await db.Locations.Select(l => l.Id).ToListAsync();

        // 7. PRODUCTS (200)
        if (!await db.Products.AnyAsync())
        {
            var unitIds = await db.Units.Select(u => u.Id).ToListAsync();
            var brandIds = await db.Brands.Select(b => b.Id).ToListAsync();
            var catIds = await db.Categories.Select(c => c.Id).ToListAsync();
            var supIds = await db.Suppliers.Select(s => s.Id).ToListAsync();

            var productNames = new[] { "ABS", "PC", "PA66", "PP", "POM" };

            var products = Enumerable.Range(1, 200).Select(i => new Product
            {
                Code = $"P{i:D5}",
                Name = $"Nhựa {productNames[i % productNames.Length]} Grade {i}",
                Description = $"Mô tả chi tiết sản phẩm nhựa kỹ thuật số {i} - cao cấp, độ bền cao",
                IsActive = true,
                Type = ProductType.Production,                    // or ProductType.Material depending on your need
                CategoryId = catIds[(i - 1) % catIds.Count],
                BrandId = brandIds[(i - 1) % brandIds.Count],
                UnitId = unitIds[(i - 1) % unitIds.Count],
                SupplierId = supIds[(i - 1) % supIds.Count],
                CreatedAt = RandomPastDate()
            }).ToList();

            db.Products.AddRange(products);
            await db.SaveChangesAsync();
            Console.WriteLine("→ Products seeded (200)");
        }

        var productIds = await db.Products.Select(p => p.Id).ToListAsync();

        // 8. LOTS – 1 to 3 lots per product
        if (!await db.Lots.AnyAsync())
        {
            var lots = new List<Lot>();
            foreach (var prodId in productIds)
            {
                var lotCount = random.Next(1, 4);
                for (int j = 0; j < lotCount; j++)
                {
                    lots.Add(new Lot
                    {
                        Id = Guid.NewGuid(),
                        Code = $"LOT-{prodId}-{random.Next(10000, 99999)}-{j + 1}",
                        productId = prodId,
                        ExpiryDate = baseDate.AddMonths(random.Next(12, 60)),
                        ManufacturingDate = baseDate.AddMonths(-random.Next(1, 18)),
                        CreatedAt = RandomPastDate()
                    });
                }
            }

            db.Lots.AddRange(lots);
            await db.SaveChangesAsync();
            Console.WriteLine($"→ {lots.Count} Lots seeded");
        }

        var lotDict = await db.Lots
            .GroupBy(l => l.productId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(l => l.Id).ToList());

        // 9. INVENTORIES – at least one record per product
        if (!await db.Inventories.AnyAsync())
        {
            var inventories = new List<Inventory>();
            foreach (var prodId in productIds.Take(200)) // limit for faster testing if needed
            {
                var lotIds = lotDict.GetValueOrDefault(prodId, new List<Guid>());
                if (!lotIds.Any()) continue;

                var selectedLotId = lotIds[random.Next(lotIds.Count)];

                inventories.Add(new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseIds[random.Next(warehouseIds.Count)],
                    LocationId = locationIds[random.Next(locationIds.Count)], // can be null if you want
                    LotId = selectedLotId,
                    ProductId = prodId,
                    OnHandQuantity = Math.Round((decimal)(random.NextDouble() * 4800 + 200), 4), // 200 → 5000 kg
                    LockedQuantity = 0m,
                    InTransitQuantity = 0m,
                    CreatedAt = baseDate,
                    UpdatedAt = null
                });
            }

            db.Inventories.AddRange(inventories);
            await db.SaveChangesAsync();
            Console.WriteLine($"→ {inventories.Count} Inventory records seeded");
        }

        Console.WriteLine("\n───────────────────────────────────────────────");
        Console.WriteLine("✅ HOÀN TẤT SEED DỮ LIỆU KHO NHỰA KỸ THUẬT");
        Console.WriteLine("All enum casting issues have been resolved.");
        Console.WriteLine("───────────────────────────────────────────────");
    }
}