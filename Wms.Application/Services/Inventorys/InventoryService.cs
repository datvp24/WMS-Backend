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

        // =========================
        // GET INVENTORY
        // =========================
        public async Task<InventoryDto?> GetAsync(Guid id)
            => await _db.Inventories
                .Where(x => x.Id == id)
                .Select(x => new InventoryDto
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    ProductId = x.ProductId,
                    OnHandQuantity = x.OnHandQuantity,
                    LockedQuantity = x.LockedQuantity,
                    InTransitQuantity = x.InTransitQuantity
                })
                .FirstOrDefaultAsync();

        public async Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto)
        {
            var query = _db.Inventories.AsQueryable();

            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId);

            if (dto.LocationId.HasValue)
                query = query.Where(x => x.LocationId == dto.LocationId);

            if (dto.ProductId.HasValue)
                query = query.Where(x => x.ProductId == dto.ProductId);

            if (dto.ProductIds != null && dto.ProductIds.Any())
                query = query.Where(x => dto.ProductIds.Contains(x.ProductId));

            return await query
                .Select(x => new InventoryDto
                {
                    Id = x.Id,
                    WarehouseId = x.WarehouseId,
                    LocationId = x.LocationId,
                    ProductId = x.ProductId,
                    OnHandQuantity = x.OnHandQuantity,
                    LockedQuantity = x.LockedQuantity,
                    InTransitQuantity = x.InTransitQuantity
                })
                .ToListAsync();
        }

        // =========================
        // INVENTORY HISTORY
        // =========================
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
                    Note = x.Note,
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync();

        // =========================
        // ADJUST INVENTORY
        // =========================
        public async Task AdjustAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qtyChange,
            InventoryActionType actionType,
            string? refCode,
            string? note = null)
        {
            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
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
                    OnHandQuantity = 0,
                    LockedQuantity = 0,
                    InTransitQuantity = 0
                };
                _db.Inventories.Add(inv);
            }

            if (inv.OnHandQuantity + qtyChange < 0)
                throw new Exception("Not enough stock");

            inv.OnHandQuantity += qtyChange;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = qtyChange,
                ActionType = actionType,
                ReferenceCode = refCode,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        // =========================
        // LOCK STOCK
        // =========================
        public async Task LockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null)
        {
            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductId == productId);

            if (inv == null)
                throw new Exception("Inventory not found");

            if (inv.OnHandQuantity - inv.LockedQuantity < qty)
                throw new Exception("Not enough available stock");

            inv.LockedQuantity += qty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = qty,
                ActionType = InventoryActionType.Lock,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }

        // =========================
        // UNLOCK STOCK
        // =========================
        public async Task UnlockStockAsync(
            Guid warehouseId,
            Guid locationId,
            int productId,
            decimal qty,
            string? note = null)
        {
            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == locationId &&
                    x.ProductId == productId);

            if (inv == null)
                throw new Exception("Inventory not found");

            if (inv.LockedQuantity < qty)
                throw new Exception("Cannot unlock more than locked quantity");

            inv.LockedQuantity -= qty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = -qty,
                ActionType = InventoryActionType.Unlock,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
    }
}
