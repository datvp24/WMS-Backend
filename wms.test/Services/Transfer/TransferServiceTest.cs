using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Application.Services.Transfer;
using Wms.Application.DTOS.Transfer;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Domain.Enums.Transfer;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// FIX: Sử dụng alias để tránh conflict namespace vs class name
using InventoryEntity = Wms.Domain.Entity.Inventorys.Inventory;
using InventoryHistoryEntity = Wms.Domain.Entity.Inventorys.InventoryHistory;
using TransferOrderEntity = Wms.Domain.Entity.Transfer.TransferOrder;
using TransferOrderItemEntity = Wms.Domain.Entity.Transfer.TransferOrderItem;
using WarehouseEntity = Wms.Domain.Entity.Warehouses.Warehouse;
using LocationEntity = Wms.Domain.Entity.Warehouses.Location;
using ProductEntity = Wms.Domain.Entity.MasterData.Product;

namespace Wms.Tests.Services.Transfer
{
    public class TransferServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly Mock<IInventoryService> _inventoryServiceMock;
        private readonly Mock<IJwtService> _jwtServiceMock;
        private readonly TransferService _transferService;

        public TransferServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _inventoryServiceMock = new Mock<IInventoryService>();
            _jwtServiceMock = new Mock<IJwtService>();

            _jwtServiceMock.Setup(x => x.GetUserId()).Returns(1);

            _transferService = new TransferService(
                _context,
                _inventoryServiceMock.Object,
                _jwtServiceMock.Object
            );
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Case 36: Tạo phiếu chuyển kho

        [Fact]
        public async Task CreateTransferAsync_ValidData_CreatesTransferAndLocksStock()
        {
            // Arrange
            var fromWarehouseId = Guid.NewGuid();
            var toWarehouseId = Guid.NewGuid();
            var fromLocationId = Guid.NewGuid();
            var toLocationId = Guid.NewGuid();

            var product = new ProductEntity { Id = 1, Code = "P1", Name = "Product 1" };
            _context.Products.Add(product);

            var fromWarehouse = new WarehouseEntity
            {
                Id = fromWarehouseId,
                Code = "WH1",
                Name = "Warehouse 1",
                Address = "123 Street" // THÊM Address (required)
            };
            var toWarehouse = new WarehouseEntity
            {
                Id = toWarehouseId,
                Code = "WH2",
                Name = "Warehouse 2",
                Address = "456 Avenue" // THÊM Address (required)
            };
            _context.Warehouses.AddRange(fromWarehouse, toWarehouse);

            var fromLocation = new LocationEntity
            {
                Id = fromLocationId,
                WarehouseId = fromWarehouseId,
                Code = "A1-01-01",
                Description = "From Location"
            };
            var toLocation = new LocationEntity
            {
                Id = toLocationId,
                WarehouseId = toWarehouseId,
                Code = "B1-01-01",
                Description = "To Location"
            };
            _context.Locations.AddRange(fromLocation, toLocation);

            _context.Inventories.Add(new InventoryEntity
            {
                WarehouseId = fromWarehouseId,
                LocationId = fromLocationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 0
            });
            await _context.SaveChangesAsync();

            var dto = new TransferOrderDto
            {
                FromWarehouseId = fromWarehouseId,
                ToWarehouseId = toWarehouseId,
                Note = "Transfer for stock balancing",
                Items = new List<TransferOrderItemDto>
                {
                    new TransferOrderItemDto
                    {
                        ProductId = 1,
                        FromLocationId = fromLocationId,
                        ToLocationId = toLocationId,
                        Quantity = 50
                    }
                }
            };

            // Act
            var result = await _transferService.CreateTransferAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Status.Should().Be(TransferStatus.Draft.ToString());
            result.Code.Should().MatchRegex(@"TRF-\d{8}-\d{4}");

            var inventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == fromLocationId);
            inventory!.LockedQuantity.Should().Be(50);

            var lockHistory = await _context.InventoryHistories
                .FirstOrDefaultAsync(h => h.ActionType == InventoryActionType.Lock);
            lockHistory.Should().NotBeNull();
        }

        [Fact]
        public async Task CreateTransferAsync_SameSourceAndDestination_ThrowsException()
        {
            // Arrange
            var warehouseId = Guid.NewGuid();
            var locationId = Guid.NewGuid();

            var dto = new TransferOrderDto
            {
                FromWarehouseId = warehouseId,
                ToWarehouseId = warehouseId,
                Items = new List<TransferOrderItemDto>
                {
                    new TransferOrderItemDto
                    {
                        FromLocationId = locationId,
                        ToLocationId = locationId,
                        Quantity = 10
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _transferService.CreateTransferAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Contain("Vị trí nguồn và đích không được trùng nhau"));
        }

        [Fact]
        public async Task CreateTransferAsync_InsufficientStock_ThrowsException()
        {
            // Arrange
            var fromLocationId = Guid.NewGuid();
            var toLocationId = Guid.NewGuid();

            _context.Inventories.Add(new InventoryEntity
            {
                WarehouseId = Guid.NewGuid(),
                LocationId = fromLocationId,
                ProductId = 1,
                OnHandQuantity = 30,
                LockedQuantity = 20 // Available = 10
            });
            await _context.SaveChangesAsync();

            var dto = new TransferOrderDto
            {
                FromWarehouseId = Guid.NewGuid(),
                ToWarehouseId = Guid.NewGuid(),
                Items = new List<TransferOrderItemDto>
                {
                    new TransferOrderItemDto
                    {
                        ProductId = 1,
                        FromLocationId = fromLocationId,
                        ToLocationId = toLocationId,
                        Quantity = 20 // Trying to transfer more than available
                    }
                }
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _transferService.CreateTransferAsync(dto)
            ).ContinueWith(t => t.Result.Message.Should().Contain("không đủ tồn kho"));
        }

        #endregion

        #region Case 37: Duyệt phiếu chuyển kho

        [Fact]
        public async Task ApproveTransferAsync_ValidDraftTransfer_CompletesTransfer()
        {
            // Arrange
            var fromWarehouseId = Guid.NewGuid();
            var toWarehouseId = Guid.NewGuid();
            var fromLocationId = Guid.NewGuid();
            var toLocationId = Guid.NewGuid();

            var product = new ProductEntity { Id = 1, Code = "P1", Name = "Product 1" };
            _context.Products.Add(product);

            var fromWarehouse = new WarehouseEntity
            {
                Id = fromWarehouseId,
                Code = "WH1",
                Name = "Warehouse 1",
                Address = "123 Street" // THÊM
            };
            var toWarehouse = new WarehouseEntity
            {
                Id = toWarehouseId,
                Code = "WH2",
                Name = "Warehouse 2",
                Address = "456 Avenue" // THÊM
            };
            _context.Warehouses.AddRange(fromWarehouse, toWarehouse);

            var fromLocation = new LocationEntity
            {
                Id = fromLocationId,
                WarehouseId = fromWarehouseId,
                Code = "A1-01-01",
                Description = "From Location"
            };
            var toLocation = new LocationEntity
            {
                Id = toLocationId,
                WarehouseId = toWarehouseId,
                Code = "B1-01-01",
                Description = "To Location"
            };
            _context.Locations.AddRange(fromLocation, toLocation);

            var transfer = new TransferOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = "TRF-001",
                FromWarehouseId = fromWarehouseId,
                ToWarehouseId = toWarehouseId,
                Status = TransferStatus.Draft,
                Note = "Test transfer", // THÊM Note (required)
                Items = new List<TransferOrderItemEntity>
                {
                    new TransferOrderItemEntity
                    {
                        ProductId = 1,
                        FromLocationId = fromLocationId,
                        ToLocationId = toLocationId,
                        Quantity = 50
                    }
                }
            };
            _context.TransferOrders.Add(transfer);

            _context.Inventories.Add(new InventoryEntity
            {
                WarehouseId = fromWarehouseId,
                LocationId = fromLocationId,
                ProductId = 1,
                OnHandQuantity = 100,
                LockedQuantity = 50
            });
            await _context.SaveChangesAsync();

            // FIX: Mock AdjustAsync với signature đúng
            _inventoryServiceMock
                .Setup(x => x.AdjustAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<decimal>(),
                    It.IsAny<InventoryActionType>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>()
                ))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _transferService.ApproveTransferAsync(transfer.Id);

            // Assert
            result.Status.Should().Be(TransferStatus.Approved.ToString());

            var sourceInventory = await _context.Inventories
                .FirstOrDefaultAsync(i => i.LocationId == fromLocationId);
            sourceInventory!.OnHandQuantity.Should().Be(50); // 100 - 50
            sourceInventory.LockedQuantity.Should().Be(0); // Unlocked

            var transferOutHistory = await _context.InventoryHistories
                .FirstOrDefaultAsync(h => h.ActionType == InventoryActionType.TransferOut);
            transferOutHistory.Should().NotBeNull();
            transferOutHistory!.QuantityChange.Should().Be(-50);

            _inventoryServiceMock.Verify(
                x => x.AdjustAsync(
                    toWarehouseId,
                    toLocationId,
                    1,
                    50,
                    InventoryActionType.TransferIn,
                    "TRF-001",
                    It.IsAny<string>(),
                    It.IsAny<Guid?>(),
                    It.IsAny<DateTime?>(),
                    It.IsAny<string>()
                ),
                Times.Once
            );
        }

        [Fact]
        public async Task ApproveTransferAsync_WrongStatus_ThrowsException()
        {
            // Arrange
            var transfer = new TransferOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = "TRF-001",
                Status = TransferStatus.Approved,
                Note = "" // THÊM Note (có thể empty string)
            };
            _context.TransferOrders.Add(transfer);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _transferService.ApproveTransferAsync(transfer.Id)
            ).ContinueWith(t => t.Result.Message.Should().Contain("Chỉ có thể duyệt phiếu ở trạng thái Nháp"));
        }

        [Fact]
        public async Task ApproveTransferAsync_InsufficientStockOnApproval_RollbacksTransaction()
        {
            // Arrange
            var fromLocationId = Guid.NewGuid();
            var toLocationId = Guid.NewGuid();

            var transfer = new TransferOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = "TRF-001",
                FromWarehouseId = Guid.NewGuid(),
                ToWarehouseId = Guid.NewGuid(),
                Status = TransferStatus.Draft,
                Note = "Test", // THÊM Note
                Items = new List<TransferOrderItemEntity>
                {
                    new TransferOrderItemEntity
                    {
                        ProductId = 1,
                        FromLocationId = fromLocationId,
                        ToLocationId = toLocationId,
                        Quantity = 100
                    }
                }
            };
            _context.TransferOrders.Add(transfer);

            // Not enough stock
            _context.Inventories.Add(new InventoryEntity
            {
                WarehouseId = transfer.FromWarehouseId,
                LocationId = fromLocationId,
                ProductId = 1,
                OnHandQuantity = 50, // Less than required
                LockedQuantity = 0
            });
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _transferService.ApproveTransferAsync(transfer.Id)
            ).ContinueWith(t => t.Result.Message.Should().Contain("không đủ tồn kho"));

            // Verify rollback - status should remain Draft
            var transferAfter = await _context.TransferOrders.FindAsync(transfer.Id);
            transferAfter!.Status.Should().Be(TransferStatus.Draft);
        }

        #endregion

        #region Case 38: Hủy phiếu chuyển kho

        [Fact]
        public async Task CancelTransferAsync_DraftTransfer_CancelsSuccessfully()
        {
            // Arrange
            var transfer = new TransferOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = "TRF-001",
                Status = TransferStatus.Draft,
                Note = "" // THÊM Note
            };
            _context.TransferOrders.Add(transfer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _transferService.CancelTransferAsync(transfer.Id);

            // Assert
            result.Status.Should().Be(TransferStatus.Cancelled.ToString());

            var cancelled = await _context.TransferOrders.FindAsync(transfer.Id);
            cancelled!.Status.Should().Be(TransferStatus.Cancelled);
            cancelled.UpdatedBy.Should().Be(1);
            cancelled.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task CancelTransferAsync_ApprovedTransfer_ThrowsException()
        {
            // Arrange
            var transfer = new TransferOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = "TRF-001",
                Status = TransferStatus.Approved,
                Note = "" // THÊM Note
            };
            _context.TransferOrders.Add(transfer);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await _transferService.CancelTransferAsync(transfer.Id)
            ).ContinueWith(t => t.Result.Message.Should().Be("Phiếu đã duyệt không thể hủy."));
        }

        #endregion

        #region Case 39: Truy vấn

        [Fact]
        public async Task GetTransfersAsync_WithStatusFilter_ReturnsFilteredResults()
        {
            // Arrange
            var warehouse1 = new WarehouseEntity
            {
                Id = Guid.NewGuid(),
                Code = "WH1",
                Name = "Warehouse 1",
                Address = "123 Street" // THÊM
            };
            var warehouse2 = new WarehouseEntity
            {
                Id = Guid.NewGuid(),
                Code = "WH2",
                Name = "Warehouse 2",
                Address = "456 Avenue" // THÊM
            };
            _context.Warehouses.AddRange(warehouse1, warehouse2);

            _context.TransferOrders.AddRange(
                new TransferOrderEntity
                {
                    Code = "TRF-001",
                    FromWarehouseId = warehouse1.Id,
                    ToWarehouseId = warehouse2.Id,
                    Status = TransferStatus.Approved,
                    Note = "Approved transfer", // THÊM
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new TransferOrderEntity
                {
                    Code = "TRF-002",
                    FromWarehouseId = warehouse1.Id,
                    ToWarehouseId = warehouse2.Id,
                    Status = TransferStatus.Draft,
                    Note = "Draft transfer", // THÊM
                    CreatedAt = DateTime.UtcNow
                }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _transferService.GetTransfersAsync(1, 20, "Approved");

            // Assert
            result.Should().HaveCount(1);
            result.First().Status.Should().Be("Approved");
        }

        [Fact]
        public async Task GetTransferByIdAsync_ValidId_ReturnsDetailedInfo()
        {
            // Arrange
            var fromWarehouse = new WarehouseEntity
            {
                Id = Guid.NewGuid(),
                Code = "WH1",
                Name = "Warehouse From",
                Address = "123 From Street" // THÊM
            };
            var toWarehouse = new WarehouseEntity
            {
                Id = Guid.NewGuid(),
                Code = "WH2",
                Name = "Warehouse To",
                Address = "456 To Avenue" // THÊM
            };
            var product = new ProductEntity { Id = 1, Code = "P1", Name = "Product 1" };
            var fromLocation = new LocationEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = fromWarehouse.Id,
                Code = "A1-01-01",
                Description = "From Location"
            };
            var toLocation = new LocationEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = toWarehouse.Id,
                Code = "B1-01-01",
                Description = "To Location"
            };

            _context.Warehouses.AddRange(fromWarehouse, toWarehouse);
            _context.Products.Add(product);
            _context.Locations.AddRange(fromLocation, toLocation);

            var transfer = new TransferOrderEntity
            {
                Id = Guid.NewGuid(),
                Code = "TRF-001",
                FromWarehouseId = fromWarehouse.Id,
                ToWarehouseId = toWarehouse.Id,
                Status = TransferStatus.Draft,
                Note = "Test transfer", // THÊM
                Items = new List<TransferOrderItemEntity>
                {
                    new TransferOrderItemEntity
                    {
                        ProductId = 1,
                        FromLocationId = fromLocation.Id,
                        ToLocationId = toLocation.Id,
                        Quantity = 50
                    }
                }
            };
            _context.TransferOrders.Add(transfer);
            await _context.SaveChangesAsync();

            // Act
            var result = await _transferService.GetTransferByIdAsync(transfer.Id);

            // Assert
            result.Should().NotBeNull();
            result.FromWarehouseName.Should().Be("Warehouse From");
            result.ToWarehouseName.Should().Be("Warehouse To");
            result.Items.Should().HaveCount(1);
            result.Items.First().ProductName.Should().Be("Product 1");
            result.Items.First().FromLocationCode.Should().Be("A1-01-01");
            result.Items.First().ToLocationCode.Should().Be("B1-01-01");
        }

        #endregion
    }
}