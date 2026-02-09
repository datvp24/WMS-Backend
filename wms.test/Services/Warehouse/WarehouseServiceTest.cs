using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.Warehouses;
using Wms.Application.DTOS.Warehouse;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Enums.location;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Wms.Tests.Services.Warehouse
{
    public class WarehouseServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly WarehouseService _warehouseService;

        public WarehouseServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _warehouseService = new WarehouseService(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 40: Quản lý kho

        [Fact]
        public async Task CreateAsync_ValidData_CreatesWarehouseWithTrimmedUpperCode()
        {
            // Arrange
            var dto = new WarehouseCreateDto
            {
                Code = " wh-hcm-01 ",
                Name = "Warehouse HCM",
                WarehouseType = WarehouseType.FinishedGoods,
                Address = "123 HCM Street"
            };

            // Act
            var result = await _warehouseService.CreateAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("WH-HCM-01");
            result.Name.Should().Be("Warehouse HCM");
            result.Status.Should().Be(WarehouseStatus.Active);
        }

        [Fact]
        public async Task CreateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Warehouses.Add(new Domain.Entity.Warehouses.Warehouse
            {
                Code = "WH-HCM-01",
                Name = "Existing Warehouse"
            });
            await _context.SaveChangesAsync();

            var dto = new WarehouseCreateDto
            {
                Code = "WH-HCM-01",
                Name = "New Warehouse"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _warehouseService.CreateAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Be("Warehouse code already exists."));
        }

        [Fact]
        public async Task LockAsync_ValidWarehouse_LocksWarehouse()
        {
            // Arrange
            var warehouse = new Domain.Entity.Warehouses.Warehouse
            {
                Id = Guid.NewGuid(),
                Code = "WH-01",
                Name = "Test Warehouse",
                Status = WarehouseStatus.Active
            };
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            // Act
            await _warehouseService.LockAsync(warehouse.Id);

            // Assert
            var locked = await _context.Warehouses.FindAsync(warehouse.Id);
            locked!.Status.Should().Be(WarehouseStatus.Locked);
            locked.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task DeleteAsync_WarehouseWithLocations_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var warehouse = new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouseId,
                Code = "WH-01",
                Name = "Test Warehouse"
            };
            _context.Warehouses.Add(warehouse);

            _context.Locations.Add(new Location
            {
                WarehouseId = warehouseId,
                Code = "A1-01-01"
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _warehouseService.DeleteAsync(warehouseId)
            ).ContinueWith(t => t.Result.Message.Should().Be("Cannot delete a warehouse that still has locations."));
        }

        #endregion

        #region Case 41: Quản lý vị trí

        [Fact]
        public async Task CreateLocationAsync_ValidFormat_CreatesLocation()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            _context.Warehouses.Add(new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouseId,
                Code = "WH-01",
                Name = "Test Warehouse",
                Status = WarehouseStatus.Active
            });
            await _context.SaveChangesAsync();

            var dto = new LocationCreateDto
            {
                WarehouseId = warehouseId,
                Code = "A1-02-05",
                Type = LocationType.Storage,
                Description = "Storage Location"
            };

            // Act
            var result = await _warehouseService.CreateLocationAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Code.Should().Be("A1-02-05");
            result.Type.Should().Be(LocationType.Storage);
        }

        [Fact]
        public async Task CreateLocationAsync_InvalidFormat_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            _context.Warehouses.Add(new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouseId,
                Code = "WH-01",
                Status = WarehouseStatus.Active
            });
            await _context.SaveChangesAsync();

            var dto = new LocationCreateDto
            {
                WarehouseId = warehouseId,
                Code = "Khu-A-01", // Invalid format
                Type = LocationType.Storage
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _warehouseService.CreateLocationAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Contain("Location code invalid"));
        }

        [Fact]
        public async Task CreateLocationAsync_LockedWarehouse_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            _context.Warehouses.Add(new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouseId,
                Code = "WH-01",
                Status = WarehouseStatus.Locked
            });
            await _context.SaveChangesAsync();

            var dto = new LocationCreateDto
            {
                WarehouseId = warehouseId,
                Code = "A1-01-01",
                Type = LocationType.Storage
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _warehouseService.CreateLocationAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Contain("Cannot create location while warehouse is locked"));
        }

        [Fact]
        public async Task GetReceivingLocationId_ValidWarehouse_ReturnsLocationId()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var receivingLocationId = Guid.NewGuid();

            _context.Locations.Add(new Location
            {
                Id = receivingLocationId,
                WarehouseId = warehouseId,
                Type = LocationType.Receiving,
                Code = "RECEIVING",
                IsActive = true
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _warehouseService.GetReceivingLocationId(warehouseId);

            // Assert
            result.Should().Be(receivingLocationId);
        }

        [Fact]
        public async Task GetIssuedLocationId_ValidWarehouse_ReturnsLocation()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var shippingLocation = new Location
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                Type = LocationType.Shipping,
                Code = "SHIPPING",
                IsActive = true
            };

            _context.Locations.Add(shippingLocation);
            await _context.SaveChangesAsync();

            // Act
            var result = await _warehouseService.GetIssuedLocationId(warehouseId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(shippingLocation.Id);
            result.Type.Should().Be(LocationType.Shipping);
        }

        #endregion

        #region Case 42: Truy vấn và phân loại

        [Fact]
        public async Task GetWarehousesByProduct_ProductExists_ReturnsWarehouses()
        {
            // Arrange
            var warehouse1Id = Guid.NewGuid();
            var warehouse2Id = Guid.NewGuid();

            var warehouse1 = new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouse1Id,
                Code = "WH1",
                Name = "Warehouse 1"
            };
            var warehouse2 = new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouse2Id,
                Code = "WH2",
                Name = "Warehouse 2"
            };

            _context.Warehouses.AddRange(warehouse1, warehouse2);

            _context.Inventories.AddRange(
                new Domain.Entity.Inventorys.Inventory { WarehouseId = warehouse1Id, ProductId = 1, LocationId = Guid.NewGuid() },
                new Domain.Entity.Inventorys.Inventory { WarehouseId = warehouse2Id, ProductId = 1, LocationId = Guid.NewGuid() }
            );
            await _context.SaveChangesAsync();

            var dto = new WarehousesbyProduct { ProductId = 1 };

            // Act
            var result = await _warehouseService.GetWarehousesByProduct(dto);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(w => w.Code == "WH1");
            result.Should().Contain(w => w.Code == "WH2");
        }

        [Fact]
        public async Task GetByWarehouseType_ValidType_ReturnsFilteredWarehouses()
        {
            // Arrange
            _context.Warehouses.AddRange(
                new Domain.Entity.Warehouses.Warehouse
                {
                    Code = "WH-RM-01",
                    Name = "Raw Material 1",
                    WarehouseType = WarehouseType.RawMaterial
                },
                new Domain.Entity.Warehouses.Warehouse
                {
                    Code = "WH-RM-02",
                    Name = "Raw Material 2",
                    WarehouseType = WarehouseType.RawMaterial
                },
                new Domain.Entity.Warehouses.Warehouse
                {
                    Code = "WH-FG-01",
                    Name = "Finished Goods",
                    WarehouseType = WarehouseType.FinishedGoods
                }
            );
            await _context.SaveChangesAsync();

            var dto = new WarehousesbyTypeDto { warehousetype = WarehouseType.RawMaterial };

            // Act
            var result = await _warehouseService.GetByWarehouseType(dto);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(w => w.WarehouseType.Should().Be(WarehouseType.RawMaterial));
        }

        [Fact]
        public async Task QueryAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            for (int i = 1; i <= 15; i++)
            {
                _context.Warehouses.Add(new Domain.Entity.Warehouses.Warehouse
                {
                    Code = $"WH-{i:D2}",
                    Name = $"Warehouse {i}",
                    CreatedAt = DateTime.UtcNow.AddDays(-i)
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _warehouseService.QueryAsync(
                page: 1,
                pageSize: 10,
                q: null,
                sortBy: null,
                asc: false
            );

            // Assert
            items.Should().HaveCount(10);
            total.Should().Be(15);
        }

        [Fact]
        public async Task QueryAsync_WithSearch_FiltersResults()
        {
            // Arrange
            _context.Warehouses.AddRange(
                new Domain.Entity.Warehouses.Warehouse { Code = "WH-HCM-01", Name = "Ho Chi Minh Warehouse" },
                new Domain.Entity.Warehouses.Warehouse { Code = "WH-HN-01", Name = "Ha Noi Warehouse" },
                new Domain.Entity.Warehouses.Warehouse { Code = "WH-DN-01", Name = "Da Nang Warehouse" }
            );
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _warehouseService.QueryAsync(
                page: 1,
                pageSize: 10,
                q: "HCM",
                sortBy: null,
                asc: false
            );

            // Assert
            items.Should().HaveCount(1);
            items.First().Code.Should().Be("WH-HCM-01");
        }

        [Fact]
        public async Task QueryAsync_WithSorting_SortsCorrectly()
        {
            // Arrange
            _context.Warehouses.AddRange(
                new Domain.Entity.Warehouses.Warehouse { Code = "WH-C", Name = "Warehouse C" },
                new Domain.Entity.Warehouses.Warehouse { Code = "WH-A", Name = "Warehouse A" },
                new Domain.Entity.Warehouses.Warehouse { Code = "WH-B", Name = "Warehouse B" }
            );
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _warehouseService.QueryAsync(
                page: 1,
                pageSize: 10,
                q: null,
                sortBy: "name",
                asc: true
            );

            // Assert
            items.First().Name.Should().Be("Warehouse A");
            items.Last().Name.Should().Be("Warehouse C");
        }

        [Fact]
        public async Task QueryAsync_WithPageSizeZero_ReturnsAllItems()
        {
            // Arrange
            for (int i = 1; i <= 20; i++)
            {
                _context.Warehouses.Add(new Domain.Entity.Warehouses.Warehouse
                {
                    Code = $"WH-{i:D2}",
                    Name = $"Warehouse {i}"
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var (items, total) = await _warehouseService.QueryAsync(
                page: 1,
                pageSize: 0, // Get all
                q: null,
                sortBy: null,
                asc: false
            );

            // Assert
            items.Should().HaveCount(20);
            total.Should().Be(20);
        }

        #endregion

        #region Update and Delete Tests

        [Fact]
        public async Task UpdateAsync_ValidData_UpdatesWarehouse()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var warehouse = new Domain.Entity.Warehouses.Warehouse
            {
                Id = warehouseId,
                Code = "WH-OLD",
                Name = "Old Name",
                Address = "Old Address",
                Status = WarehouseStatus.Active
            };
            _context.Warehouses.Add(warehouse);
            await _context.SaveChangesAsync();

            var dto = new WarehouseUpdateDto
            {
                Id = warehouseId,
                Code = " WH-NEW ",
                Name = "New Name",
                Address = "New Address",
                Status = WarehouseStatus.Maintenance
            };

            // Act
            var result = await _warehouseService.UpdateAsync(dto);

            // Assert
            result.Code.Should().Be("WH-NEW");
            result.Name.Should().Be("New Name");
            result.Status.Should().Be(WarehouseStatus.Maintenance);

            var updated = await _context.Warehouses.FindAsync(warehouseId);
            updated!.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task UpdateAsync_DuplicateCode_ThrowsException()
        {
            // Arrange
            _context.Warehouses.AddRange(
                new Domain.Entity.Warehouses.Warehouse { Id = Guid.NewGuid(), Code = "WH-01", Name = "Warehouse 1" },
                new Domain.Entity.Warehouses.Warehouse { Id = Guid.NewGuid(), Code = "WH-02", Name = "Warehouse 2" }
            );
            await _context.SaveChangesAsync();

            var warehouse2 = await _context.Warehouses.FirstAsync(w => w.Code == "WH-02");

            var dto = new WarehouseUpdateDto
            {
                Id = warehouse2.Id,
                Code = "WH-01", // Trying to use existing code
                Name = "Updated Name"
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _warehouseService.UpdateAsync(dto)
            );
        }

        #endregion
    }
}