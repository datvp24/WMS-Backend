using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Inventorys
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _db;
        public InventoryService(AppDbContext db) => _db = db;

        public async Task<InventoryDto?> GetAsync(Guid id)
            => await _db.Inventories
                .Where(x => x.Id == id)
                .Select(x => new InventoryDto
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    LockedQuantity = x.LockedQuantity
                }).FirstOrDefaultAsync();

        public async Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto)
        {
            var query = _db.Inventories.AsQueryable();
            if (dto.WarehouseId != null) query = query.Where(x => x.WarehouseId == dto.WarehouseId);
            if (dto.LocationId != null) query = query.Where(x => x.LocationId == dto.LocationId);
            if (dto.ProductId != null) query = query.Where(x => x.ProductId == dto.ProductId);

            return await query.Select(x => new InventoryDto
            {
                Id = x.Id,
                WarehouseId = x.WarehouseId,
                LocationId = x.LocationId,
                ProductId = x.ProductId,
                Quantity = x.Quantity,
                LockedQuantity = x.LockedQuantity
            }).ToListAsync();
        }

        public Task<List<InventoryDto>> GetByProductAsync(int productId) => QueryAsync(new InventoryQueryDto { ProductId = productId });
        public Task<List<InventoryDto>> GetByWarehouseAsync(Guid warehouseId) => QueryAsync(new InventoryQueryDto { WarehouseId = warehouseId });
        public Task<List<InventoryDto>> GetByLocationAsync(Guid locationId) => QueryAsync(new InventoryQueryDto { LocationId = locationId });

        public async Task<List<InventoryHistoryDto>> GetHistoryAsync(int productId)
            => await _db.InventoryHistories
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new InventoryHistoryDto
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    ProductId = x.ProductId,
                    QuantityChange = x.QuantityChange,
                    ActionType = x.ActionType,
                    ReferenceCode = x.ReferenceCode,
                    CreatedAt = x.CreatedAt
                }).ToListAsync();

        public async Task AdjustAsync(Guid warehouseId, Guid locationId, int productId, decimal qtyChange, InventoryActionType actionType, string? refCode)
        {
            var inv = await _db.Inventories.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId && 
                x.LocationId == locationId &&
                x.ProductId == productId);

            if (inv == null)
            {
                inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    Quantity = 0
                };
                _db.Inventories.Add(inv);
            }

            inv.Quantity += qtyChange;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = qtyChange,
                ActionType = actionType,
                ReferenceCode = refCode
            });

            await _db.SaveChangesAsync();
        }

        public async Task LockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty)
        {
            var inv = await _db.Inventories.FirstAsync(x =>
                x.WarehouseId == warehouseId &&
                x.LocationId == locationId &&
                x.ProductId == productId);

            if (inv.Quantity - inv.LockedQuantity < qty)
                throw new Exception("Not enough available stock");

            inv.LockedQuantity += qty;
            await _db.SaveChangesAsync();
        }

        public async Task UnlockStockAsync(Guid warehouseId, Guid locationId, int productId, decimal qty)
        {
            var inv = await _db.Inventories.FirstAsync(x =>
                x.WarehouseId == warehouseId &&
                x.LocationId == locationId &&
                x.ProductId == productId);

            inv.LockedQuantity -= qty;
            if (inv.LockedQuantity < 0) inv.LockedQuantity = 0;

            await _db.SaveChangesAsync();
        }
    }
}
