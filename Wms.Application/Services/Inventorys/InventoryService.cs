using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Interfaces.Services.Warehouse;

namespace Wms.Application.Services.Inventorys
{
    public class InventoryService : IInventoryService
    {
        private readonly AppDbContext _db;
        public static readonly Guid RECEIVING_LOCATION_GUID = new Guid("11111111-2222-3333-4444-555555555555");
        private readonly IWarehouseService warehouseService;

        public InventoryService(AppDbContext db, IWarehouseService warehouse)
        {
            _db = db;
            warehouseService = warehouse;
        }

        // =========================
        // GET INVENTORY
        // =========================
        public async Task<List<LocationQtyDto>> GetAvailableLocations(int productId, Guid warehouseId)
        {
            var result = await _db.Inventories
                .Where(i => i.WarehouseId == warehouseId && i.ProductId == productId)
                .Join(
                    _db.Locations,
                    inv => inv.LocationId,
                    loc => loc.Id,
                    (inv, loc) => new LocationQtyDto
                    {
                        Id = loc.Id,
                        WarehouseId = warehouseId,
                        Type = loc.Type,
                        Code = loc.Code,
                        AvailableQty = inv.AvailableQuantity,
                        Description = loc.Description,
                        IsActive = loc.IsActive,
                        CreatedAt = loc.CreatedAt,
                        UpdatedAt = loc.UpdatedAt
                    }
                )
                .ToListAsync();

            return result;
        }

        public async Task PutAway(PutawayDto dto)
        {
            if (dto.Qty <= 0) throw new Exception("Số lượng putaway phải > 0");

            // 1️⃣ Trừ ở Receiving location
            await AdjustAsync(
                dto.WarehouseId,
                dto.FromLocationId,
                dto.ProductId,
                dto.Qty, // trừ
                InventoryActionType.TransferOut,
                refCode: null,
                note: "Putaway from Receiving"
            );

            // 2️⃣ Cộng vào Storage location
             await AdjustAsync(
                dto.WarehouseId,
                dto.ToLocationId,
                dto.ProductId,
                dto.Qty, // cộng
                InventoryActionType.TransferIn,
                refCode: null,
                note: "Putaway to Storage"
            );
                await _db.SaveChangesAsync();
            
        }
        public async Task<InventoryDto?> GetAsync(Guid id)
        {
            return await _db.Inventories
                .Join(_db.Locations,
                      inv => inv.LocationId,
                      loc => loc.Id,
                      (inv, loc) => new InventoryDto
                      {
                          Id = inv.Id,
                          WarehouseId = inv.WarehouseId,
                          LocationId = inv.LocationId,
                          ProductId = inv.ProductId,
                          OnHandQuantity = inv.OnHandQuantity,
                          LockedQuantity = inv.LockedQuantity,
                          InTransitQuantity = inv.InTransitQuantity,
                          LocationType = loc.Type
                      })
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto)
        {
            var query = _db.Inventories.AsNoTracking().AsQueryable();

            // --- Giữ nguyên các phần filter của bạn ---
            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId);

            if (dto.LocationId.HasValue)
                query = query.Where(x => x.LocationId == dto.LocationId);

            if (dto.ProductId.HasValue)
                query = query.Where(x => x.ProductId == dto.ProductId);

            if (dto.ProductIds != null && dto.ProductIds.Any())
                query = query.Where(x => dto.ProductIds.Contains(x.ProductId));
            // ------------------------------------------

            return await query
                .Select(inv => new InventoryDto
                {
                    Id = inv.Id,
                    WarehouseId = inv.WarehouseId,
                    // Lấy tên kho từ bảng Warehouse
                    WarehouseName = _db.Warehouses
                        .Where(w => w.Id == inv.WarehouseId)
                        .Select(w => w.Name)
                        .FirstOrDefault(),

                    LocationId = inv.LocationId,
                    // Lấy mã vị trí và Type từ bảng Location
                    LocationCode = _db.Locations
                        .Where(l => l.Id == inv.LocationId)
                        .Select(l => l.Code)
                        .FirstOrDefault(),
                    LocationType = _db.Locations
                        .Where(l => l.Id == inv.LocationId)
                        .Select(l => l.Type)
                        .FirstOrDefault(),

                    ProductId = inv.ProductId,
                    // Lấy tên và mã SP từ bảng Product
                    ProductName = _db.Products
                        .Where(p => p.Id == inv.ProductId)
                        .Select(p => p.Name)
                        .FirstOrDefault(),
                    ProductCode = _db.Products
                        .Where(p => p.Id == inv.ProductId)
                        .Select(p => p.Code)
                        .FirstOrDefault(),

                    OnHandQuantity = inv.OnHandQuantity,
                    LockedQuantity = inv.LockedQuantity,
                    InTransitQuantity = inv.InTransitQuantity
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
            decimal qty,
            InventoryActionType actionType,
            string? refCode,
            string? note = null)
        {
            // 1. Validate đầu vào
            if (qty <= 0)
                throw new Exception("Quantity must be greater than zero");

            // 2. Xác định dấu (+ / -) dựa trên ActionType
            decimal signedQty = actionType switch
            {
                // === TĂNG TỒN ===
                InventoryActionType.Receive => qty,           // nhập kho
                InventoryActionType.TransferIn => qty,        // chuyển đến location
                InventoryActionType.AdjustIncrease => qty,    // điều chỉnh tăng
                InventoryActionType.StockTakeAdjustment => qty,

                // === GIẢM TỒN ===
                InventoryActionType.Issue => -qty,             // xuất kho
                InventoryActionType.TransferOut => -qty,       // chuyển đi location
                InventoryActionType.AdjustDecrease => -qty,    // điều chỉnh giảm
                InventoryActionType.StockCount => -qty,

                // === KHÔNG HỖ TRỢ ===
                _ => throw new Exception($"Unsupported inventory action: {actionType}")
            };

            // 3. Lấy inventory theo warehouse + location + product
            var inv = await _db.Inventories.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId &&
                x.LocationId == locationId &&
                x.ProductId == productId
            );

            // 4. Nếu chưa có inventory thì tạo mới
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
                    InTransitQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Inventories.Add(inv);
            }

            // 5. Validate không cho âm tồn
            // Chỉ validate âm tồn khi chúng ta đang thực hiện hành động GIẢM (signedQty < 0)
            if (signedQty < 0 && (inv.OnHandQuantity + signedQty < 0))
            {
                throw new Exception($"Không đủ hàng để trừ. Hiện có: {inv.OnHandQuantity}, yêu cầu trừ: {Math.Abs(signedQty)} và {inv.Id}, và {inv.LocationId}, và {inv.ProductId}, và {inv.WarehouseId}");
            }

            // 6. Update tồn kho
            inv.OnHandQuantity += signedQty;
            inv.UpdatedAt = DateTime.UtcNow;

            // 7. Ghi lịch sử inventory
            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = signedQty,
                ActionType = actionType,
                ReferenceCode = refCode,
                Note = note,
                CreatedAt = DateTime.UtcNow
            });

            // 8. Commit
            await _db.SaveChangesAsync();
        }

        public async Task Adjust1Async(  Guid warehouseId,
                                        Guid? locationId,
                                        int productId,
                                        decimal qtyChange,
                                        InventoryActionType actionType,
                                        string? refCode,
                                        string? note = null) 
        {

            // location tạm nếu null
            var effectiveLocationId = locationId ?? RECEIVING_LOCATION_GUID;

            var inv = await _db.Inventories
                .FirstOrDefaultAsync(x =>
                    x.WarehouseId == warehouseId &&
                    x.LocationId == effectiveLocationId &&
                    x.ProductId == productId);

            if (inv == null)
            {
                inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = effectiveLocationId,
                    ProductId = productId,
                    OnHandQuantity = 0,
                    LockedQuantity = 0,
                    InTransitQuantity = 0,
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
                LocationId = effectiveLocationId,
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
