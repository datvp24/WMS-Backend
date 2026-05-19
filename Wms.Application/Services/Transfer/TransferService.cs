using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Transfer;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Transfer;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.Transfer;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.Transfer;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Transfer;

public class TransferService : ITransferService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly IJwtService _jwt;

    public TransferService(AppDbContext db, IInventoryService inventoryService, IJwtService jwt)
    {
        _db = db;
        _inventoryService = inventoryService;
        _jwt = jwt;
    }

    // =========================================================
    // CREATE
    // =========================================================
    public async Task<TransferOrderDto> CreateTransferAsync(TransferOrderDto dto)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                if (dto.FromWarehouseId == dto.ToWarehouseId &&
                    dto.Items.Any(x => x.FromLocationId == x.ToLocationId))
                {
                    throw new InvalidOperationException("Vị trí nguồn và đích không được trùng nhau.");
                }

                if (dto.Items == null || !dto.Items.Any())
                    throw new InvalidOperationException("Phiếu chuyển kho phải có ít nhất 1 sản phẩm.");

                // =========================================================
                // LOCK STOCK — FIFO, bên trong transaction
                // =========================================================
                foreach (var item in dto.Items)
                {
                    if (item.Quantity <= 0)
                        throw new InvalidOperationException(
                            $"Số lượng sản phẩm ID {item.ProductId} phải lớn hơn 0.");

                    var stocks = await _db.Inventories
                        .Include(x => x.Lot)
                        .Where(x =>
                            x.LocationId == item.FromLocationId &&
                            x.ProductId == item.ProductId &&
                            (x.OnHandQuantity - x.LockedQuantity) > 0)
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync();

                    var totalAvailable = stocks.Sum(x => x.OnHandQuantity - x.LockedQuantity);

                    if (totalAvailable < item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Sản phẩm ID {item.ProductId} không đủ tồn kho tại vị trí nguồn. " +
                            $"Khả dụng: {totalAvailable}, Yêu cầu: {item.Quantity}."
                        );
                    }

                    decimal remainingQty = item.Quantity;

                    foreach (var stock in stocks)
                    {
                        if (remainingQty <= 0) break;

                        var available = stock.OnHandQuantity - stock.LockedQuantity;
                        if (available <= 0) continue;

                        var lockQty = Math.Min(available, remainingQty);

                        stock.LockedQuantity += lockQty;
                        stock.UpdatedAt = DateTime.UtcNow;
                        remainingQty -= lockQty;

                        _db.InventoryHistories.Add(new InventoryHistory
                        {
                            Id = Guid.NewGuid(),
                            WarehouseId = dto.FromWarehouseId,
                            LocationId = item.FromLocationId,
                            ProductId = item.ProductId,
                            QuantityChange = lockQty,
                            ActionType = InventoryActionType.Lock,
                            ReferenceCode = "LOCK_FOR_TRANSFER",
                            // ✅ FIX: Note NOT NULL — fallback rõ ràng
                            Note = $"Lock for transfer - Lot: {stock.Lot?.Code ?? "N/A"}",
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                // ✅ Code dùng Guid suffix — tránh duplicate khi nhiều request cùng ms
                var uniqueSuffix = Guid.NewGuid().ToString("N")[..8].ToUpper();
                var code = $"TRF-{DateTime.UtcNow:yyyyMMdd}-{uniqueSuffix}";

                var transfer = new TransferOrder
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    FromWarehouseId = dto.FromWarehouseId,
                    ToWarehouseId = dto.ToWarehouseId,
                    Status = TransferStatus.Draft,
                    // ✅ FIX: Note NOT NULL
                    Note = dto.Note ?? string.Empty,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = _jwt.GetUserId() ?? 1,
                    Items = dto.Items.Select(i => new TransferOrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = i.ProductId,
                        FromLocationId = i.FromLocationId,
                        ToLocationId = i.ToLocationId,
                        Quantity = i.Quantity,
                        // ✅ FIX: Note NOT NULL
                        Note = i.Note ?? string.Empty,
                    }).ToList()
                };

                _db.TransferOrders.Add(transfer);

                // ✅ Một lần SaveChanges duy nhất
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetTransferByIdAsync(transfer.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    // =========================================================
    // APPROVE
    // =========================================================
    public async Task<TransferOrderDto> ApproveTransferAsync(Guid transferId)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var transfer = await _db.Set<TransferOrder>()
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == transferId);

                if (transfer == null)
                    throw new InvalidOperationException("Không tìm thấy phiếu chuyển kho.");

                if (transfer.Status != TransferStatus.Draft)
                    throw new InvalidOperationException("Chỉ có thể duyệt phiếu ở trạng thái Nháp.");

                foreach (var item in transfer.Items)
                {
                    // ✅ Filter theo LockedQuantity > 0 — đúng với phần đã lock lúc Create
                    var stocks = await _db.Set<Inventory>()
                        .Include(x => x.Lot)
                        .Where(x =>
                            x.LocationId == item.FromLocationId &&
                            x.ProductId == item.ProductId &&
                            x.LockedQuantity > 0)
                        .OrderBy(x => x.CreatedAt)
                        .ToListAsync();

                    var totalLocked = stocks.Sum(x => x.LockedQuantity);

                    if (totalLocked < item.Quantity)
                    {
                        throw new InvalidOperationException(
                            $"Sản phẩm ID {item.ProductId} không đủ số lượng đã khóa. " +
                            $"Đã khóa: {totalLocked}, Cần: {item.Quantity}."
                        );
                    }

                    decimal remainingQty = item.Quantity;

                    foreach (var stock in stocks)
                    {
                        if (remainingQty <= 0) break;

                        var deductQty = Math.Min(stock.LockedQuantity, remainingQty);
                        if (deductQty <= 0) continue;

                        // 1️⃣ Trừ kho nguồn + mở khóa
                        stock.OnHandQuantity -= deductQty;
                        stock.LockedQuantity -= deductQty;
                        stock.UpdatedAt = DateTime.UtcNow;
                        remainingQty -= deductQty;

                        // 2️⃣ Cộng kho đích, giữ nguyên lot (FIFO)
                        await _inventoryService.AdjustAsync(
                            warehouseId: transfer.ToWarehouseId,
                            locationId: item.ToLocationId,
                            productId: item.ProductId,
                            qty: deductQty,
                            actionType: InventoryActionType.TransferIn,
                            lotId: stock.LotId,
                            refCode: transfer.Code,
                            // ✅ FIX: Note NOT NULL
                            note: $"Transfer from location {item.FromLocationId} - Lot: {stock.Lot?.Code ?? "N/A"}"
                        );
                    }

                    // 3️⃣ History kho nguồn (1 dòng tổng)
                    _db.InventoryHistories.Add(new InventoryHistory
                    {
                        Id = Guid.NewGuid(),
                        WarehouseId = transfer.FromWarehouseId,
                        LocationId = item.FromLocationId,
                        ProductId = item.ProductId,
                        QuantityChange = -item.Quantity,
                        ActionType = InventoryActionType.TransferOut,
                        ReferenceCode = transfer.Code,
                        // ✅ FIX: Note NOT NULL
                        Note = $"Transfer to location {item.ToLocationId}",
                        CreatedAt = DateTime.UtcNow
                    });
                }

                transfer.Status = TransferStatus.Approved;
                transfer.ApprovedAt = DateTime.UtcNow;
                transfer.ApprovedBy = _jwt.GetUserId();
                transfer.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return await GetTransferByIdAsync(transfer.Id);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    // =========================================================
    // CANCEL — Mở khóa stock đã lock lúc Create
    // =========================================================
    public async Task<TransferOrderDto> CancelTransferAsync(Guid transferId)
    {
        var transfer = await _db.Set<TransferOrder>()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == transferId);

        if (transfer == null)
            throw new InvalidOperationException("Không tìm thấy phiếu.");

        if (transfer.Status == TransferStatus.Approved)
            throw new InvalidOperationException("Phiếu đã duyệt không thể hủy.");

        if (transfer.Status == TransferStatus.Draft)
        {
            foreach (var item in transfer.Items)
            {
                var stocks = await _db.Inventories
                    .Where(x =>
                        x.LocationId == item.FromLocationId &&
                        x.ProductId == item.ProductId &&
                        x.LockedQuantity > 0)
                    .OrderBy(x => x.CreatedAt)
                    .ToListAsync();

                decimal remainingUnlock = item.Quantity;

                foreach (var stock in stocks)
                {
                    if (remainingUnlock <= 0) break;

                    var unlockQty = Math.Min(stock.LockedQuantity, remainingUnlock);
                    stock.LockedQuantity -= unlockQty;
                    stock.UpdatedAt = DateTime.UtcNow;
                    remainingUnlock -= unlockQty;

                    _db.InventoryHistories.Add(new InventoryHistory
                    {
                        Id = Guid.NewGuid(),
                        WarehouseId = transfer.FromWarehouseId,
                        LocationId = item.FromLocationId,
                        ProductId = item.ProductId,
                        QuantityChange = unlockQty,
                        ActionType = InventoryActionType.Unlock,
                        ReferenceCode = transfer.Code,
                        // ✅ FIX: Note NOT NULL
                        Note = $"Unlock due to cancellation of transfer {transfer.Code}",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        transfer.Status = TransferStatus.Cancelled;
        transfer.UpdatedAt = DateTime.UtcNow;
        transfer.UpdatedBy = _jwt.GetUserId();

        await _db.SaveChangesAsync();
        return await GetTransferByIdAsync(transferId);
    }

    // =========================================================
    // GET BY ID
    // =========================================================
    public async Task<TransferOrderDto> GetTransferByIdAsync(Guid id)
    {
        var transfer = await _db.Set<TransferOrder>()
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .Include(x => x.Items).ThenInclude(i => i.Product)
            .Include(x => x.Items).ThenInclude(i => i.FromLocation)
            .Include(x => x.Items).ThenInclude(i => i.ToLocation)
            .FirstOrDefaultAsync(x => x.Id == id);

        return transfer == null ? null! : MapToDto(transfer);
    }

    // =========================================================
    // GET LIST
    // =========================================================
    public async Task<List<TransferOrderDto>> GetTransfersAsync(
        int page = 1,
        int pageSize = 20,
        string? status = null)
    {
        var query = _db.Set<TransferOrder>()
            .Include(x => x.FromWarehouse)
            .Include(x => x.ToWarehouse)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status) &&
            Enum.TryParse<TransferStatus>(status, out var statusEnum))
        {
            query = query.Where(x => x.Status == statusEnum);
        }

        var list = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return list.Select(MapToDto).ToList();
    }

    // =========================================================
    // MAPPING HELPER
    // =========================================================
    private TransferOrderDto MapToDto(TransferOrder t) => new()
    {
        Id = t.Id,
        Code = t.Code,
        FromWarehouseId = t.FromWarehouseId,
        FromWarehouseName = t.FromWarehouse?.Name,
        ToWarehouseId = t.ToWarehouseId,
        ToWarehouseName = t.ToWarehouse?.Name,
        Status = t.Status.ToString(),
        Note = t.Note,
        CreatedAt = t.CreatedAt,
        Items = t.Items?.Select(i => new TransferOrderItemDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name,
            FromLocationId = i.FromLocationId,
            FromLocationCode = i.FromLocation?.Code,
            ToLocationId = i.ToLocationId,
            ToLocationCode = i.ToLocation?.Code,
            Quantity = i.Quantity,
            Note = i.Note
        }).ToList() ?? new()
    };
}