using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.Inventorys;
using Wms.Application.DTOS.Warehouse;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
        public async Task<List<InventoryDto>> GetInventoryByProductType(ProductType1Dto dto)
        {
            var result = await _db.Inventories
                .Where(i => i.Product.Type == dto.ProductType)
                .GroupBy(i => new
                {
                    i.ProductId,
                    i.WarehouseId
                })
                .Select(g => new InventoryDto
                {
                    ProductId = g.Key.ProductId,
                    WarehouseId = g.Key.WarehouseId,

                    WarehouseName = _db.Warehouses
                        .Where(w => w.Id == g.Key.WarehouseId)
                        .Select(w => w.Name)
                        .FirstOrDefault(),

                    ProductName = _db.Products
                        .Where(p => p.Id == g.Key.ProductId)
                        .Select(p => p.Name)
                        .FirstOrDefault(),

                    ProductCode = _db.Products
                        .Where(p => p.Id == g.Key.ProductId)
                        .Select(p => p.Code)
                        .FirstOrDefault(),

                    OnHandQuantity = g.Sum(x => x.OnHandQuantity),
                    LockedQuantity = g.Sum(x => x.LockedQuantity),
                    InTransitQuantity = g.Sum(x => x.InTransitQuantity),
                })
                .AsNoTracking()
                .ToListAsync();

            return result;
        }

        public async Task<List<InventoryDto>> QueryAsync(InventoryQueryDto dto)
        {
            // Sử dụng .AsNoTracking() để tăng tốc độ truy vấn chỉ đọc
            var query = _db.Inventories
                .Include(x => x.Product)  // Load thông tin sản phẩm
                .Include(x => x.Lot)      // Load thông tin Lô hàng (MỚI)
                .AsNoTracking()
                .AsQueryable();

            // --- Filter logic ---
            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId);

            if (dto.LocationId.HasValue)
                query = query.Where(x => x.LocationId == dto.LocationId);

            if (dto.ProductId.HasValue)
                query = query.Where(x => x.ProductId == dto.ProductId);

            if (dto.ProductIds != null && dto.ProductIds.Any())
                query = query.Where(x => dto.ProductIds.Contains(x.ProductId));

            // Thêm filter theo LotCode nếu cần
            if (!string.IsNullOrEmpty(dto.LotCode))
                query = query.Where(x => x.Lot.Code.Contains(dto.LotCode));
            // --------------------

            return await query
                .Select(inv => new InventoryDto
                {
                    Id = inv.Id,
                    WarehouseId = inv.WarehouseId,
                    // Sử dụng Navigation property giúp SQL sinh ra câu lệnh JOIN chuẩn, nhanh hơn
                    WarehouseName = _db.Warehouses
                        .Where(w => w.Id == inv.WarehouseId)
                        .Select(w => w.Name)
                        .FirstOrDefault(),

                    LocationId = inv.LocationId,
                    LocationCode = _db.Locations
                        .Where(l => l.Id == inv.LocationId)
                        .Select(l => l.Code)
                        .FirstOrDefault(),
                    LocationType = _db.Locations
                        .Where(l => l.Id == inv.LocationId)
                        .Select(l => l.Type)
                        .FirstOrDefault(),

                    ProductId = inv.ProductId,
                    ProductName = inv.Product.Name,
                    ProductCode = inv.Product.Code,

                    // --- THÔNG TIN LÔ HÀNG (MỚI) ---
                    LotId = inv.LotId,
                    LotCode = inv.Lot.Code,
                    ExpiryDate = inv.Lot.ExpiryDate,
                    // ------------------------------

                    OnHandQuantity = inv.OnHandQuantity,
                    LockedQuantity = inv.LockedQuantity,
                    InTransitQuantity = inv.InTransitQuantity,
                    // Tính toán số lượng khả dụng ngay tại query
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
     string? refCode = null,
     string? lotCode = null,   // Dùng cho Nhập kho (Mua/Sản xuất)
     Guid? lotId = null,       // Dùng cho Xuất kho/Picking
     DateTime? expiryDate = null,
     string note = null)
        {
            if (qty <= 0) throw new Exception("Số lượng phải lớn hơn 0");

            // 1. XÁC ĐỊNH LOT ID
            Guid finalLotId;
            if (lotId.HasValue)
            {
                finalLotId = lotId.Value;
            }
            else
            {
                // Logic cho Nhập kho: Tìm theo LotCode, không có thì tạo
                string code = string.IsNullOrEmpty(lotCode) ? "NOSERIAL" : lotCode;
                var lot = await _db.Lots.FirstOrDefaultAsync(x => x.productId == productId && x.Code == code);
                if (lot == null)
                {
                    lot = new Lot
                    {
                        Id = Guid.NewGuid(),
                        productId = productId,
                        Code = code,
                        ExpiryDate = expiryDate,
                        CreatedAt = DateTime.UtcNow
                    };
                    _db.Lots.Add(lot);
                    await _db.SaveChangesAsync(); // Lưu để có Lot trong DB
                }
                finalLotId = lot.Id;
            }

            // 2. TÍNH TOÁN DẤU
            decimal signedQty = (actionType == InventoryActionType.Receive ||
                                 actionType == InventoryActionType.AdjustIncrease ||
                                 actionType == InventoryActionType.TransferIn) ? qty : -qty;

            // 3. CẬP NHẬT BẢNG INVENTORY (Bộ 3: Location + Product + Lot)
            var inv = await _db.Inventories.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId && x.LocationId == locationId &&
                x.ProductId == productId && x.LotId == finalLotId);

            if (inv == null)
            {
                if (signedQty < 0) throw new Exception("Không tìm thấy tồn kho để trừ.");
                inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    LotId = finalLotId,
                    OnHandQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Inventories.Add(inv);
            }

            if (signedQty < 0 && (inv.OnHandQuantity + signedQty < 0))
                throw new Exception($"Lô hàng này tại vị trí này không đủ tồn kho (Còn: {inv.OnHandQuantity})");

            inv.OnHandQuantity += signedQty;
            inv.UpdatedAt = DateTime.UtcNow;

            // 4. GHI LỊCH SỬ (Truy vết theo Lô)
            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = signedQty,
                ActionType = actionType,
                ReferenceCode = refCode,
                CreatedAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
        }
        public async Task AdjustPickingAsync(
     Guid warehouseId,
     Guid? locationId,
     int productId,
     decimal qty,
     InventoryActionType actionType,
     string? refCode,
     Guid lotId, // <--- THÊM THAM SỐ LOTID
     string? note = null)
        {
            if (qty <= 0) throw new Exception("Quantity > 0");

            decimal signedQty = actionType switch
            {
                InventoryActionType.AdjustIncrease => qty,
                InventoryActionType.AdjustDecrease => -qty,
                _ => throw new Exception("Unsupported action")
            };

            // TÌM THEO BỘ 3: Location + Product + LotId
            var inv = await _db.Inventories.FirstOrDefaultAsync(x =>
                x.WarehouseId == warehouseId &&
                x.LocationId == locationId &&
                x.ProductId == productId &&
                x.LotId == lotId // <--- QUAN TRỌNG NHẤT
            );

            if (inv == null && signedQty < 0)
                throw new Exception($"Không tìm thấy dòng tồn kho cho Lô này tại vị trí chỉ định.");

            if (inv == null)
            {
                inv = new Inventory
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    LocationId = locationId,
                    ProductId = productId,
                    LotId = lotId,
                    OnHandQuantity = 0,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Inventories.Add(inv);
            }

            if (signedQty < 0 && (inv.OnHandQuantity + signedQty < 0))
                throw new Exception("Âm tồn kho cho Lô này!");

            inv.OnHandQuantity += signedQty;
            inv.UpdatedAt = DateTime.UtcNow;

            _db.InventoryHistories.Add(new InventoryHistory
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                LocationId = locationId,
                ProductId = productId,
                QuantityChange = signedQty,
                ActionType = actionType,
                ReferenceCode = refCode,
                CreatedAt = DateTime.UtcNow
            });

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
