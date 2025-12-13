using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Purchase;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Purchase;
using Wms.Application.Interfaces.Services;
using Wms.Domain.Entity.Purchase;
using Wms.Domain.Enums.Inventory;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Purchase;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventoryService;
    private readonly IJwtService _jwt;

    public PurchaseService(AppDbContext db, IInventoryService inventoryService, IJwtService jwt)
    {
        _db = db;
        _jwt = jwt;
        _inventoryService = inventoryService;
    }

    // ========================
    // PURCHASE ORDER
    // ========================
    public async Task<PurchaseOrderDto> CreatePOAsync(PurchaseOrderDto dto)
    {
        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            SupplierId = dto.SupplierId,
            CreateBy = _jwt.GetUserId(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = dto.Items.Select(i => new PurchaseOrderItem
            {
                Id = Guid.NewGuid(),
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }).ToList()
        };

        _db.Set<PurchaseOrder>().Add(po);
        await _db.SaveChangesAsync();

        return MapPOToDto(po);
    }

    public async Task<PurchaseOrderDto> ApprovePOAsync(Guid poId)
    {
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);
        if (po == null) throw new NotFoundException("PO not found");

        po.Status = "Approved";
        po.ApprovedAt = DateTime.UtcNow;
        po.ApprovedBy = _jwt.GetUserId();
        po.UpdatedAt = DateTime.UtcNow;
        foreach (var item in po.Items)
            item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapPOToDto(po);
    }

    public async Task<PurchaseOrderDto> RejectPOAsync(Guid poId)
    {
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);
        if (po == null) throw new NotFoundException("PO not found");

        po.Status = "Rejected";
        po.UpdatedAt = DateTime.UtcNow;
        foreach (var item in po.Items)
            item.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapPOToDto(po);
    }

    // Không paging
    public async Task<List<PurchaseOrderDto>> GetPOsAsync()
    {
        return await GetPOsAsync(1, int.MaxValue);
    }

    // Có paging + optional status filter
    public async Task<List<PurchaseOrderDto>> GetPOsAsync(int page = 1, int pageSize = 20, string? status = null)
    {
        var query = _db.Set<PurchaseOrder>().Include(x => x.Items).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(x => x.Status == status);

        var pos = await query.OrderByDescending(x => x.CreatedAt)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToListAsync();

        return pos.Select(MapPOToDto).ToList();
    }

    // ========================
    // GOODS RECEIPT
    // ========================
    public async Task<GoodsReceiptDto> CreateGRAsync(GoodsReceiptDto dto)
    {
        if (dto.WarehouseId == Guid.Empty)
            throw new BusinessRuleException("WarehouseId is required");

        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == dto.PurchaseOrderId);

        if (po == null) throw new NotFoundException("PO not found");
        if (po.Status != "Approved") throw new BusinessRuleException("Cannot create GR for PO that is not approved");

        // Check quantity
        foreach (var item in dto.Items)
        {
            var poItem = po.Items.FirstOrDefault(x => x.ProductId == item.ProductId);
            if (poItem == null) throw new BusinessRuleException($"Product {item.ProductId} not in PO");

            var totalReceived = await _db.Set<GoodsReceiptItem>()
                                         .Where(x => x.ProductId == item.ProductId && x.GoodsReceipt.PurchaseOrderId == po.Id)
                                         .SumAsync(x => x.Quantity);

            if (totalReceived + item.Quantity > poItem.Quantity)
                throw new BusinessRuleException($"Received quantity exceeds PO for product {item.ProductId}");
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var gr = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                Code = dto.Code,
                PurchaseOrderId = dto.PurchaseOrderId,
                WarehouseId = dto.WarehouseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new GoodsReceiptItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    LocationId = i.LocationId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList()
            };

            _db.Set<GoodsReceipt>().Add(gr);
            await _db.SaveChangesAsync();

            // Update inventory
            foreach (var item in gr.Items)
            {
                await _inventoryService.AdjustAsync(
                    warehouseId: gr.WarehouseId,
                    locationId: item.LocationId,
                    productId: item.ProductId,
                    qtyChange: item.Quantity,
                    actionType: InventoryActionType.Receive,
                    refCode: gr.Code
                );
            }

            await transaction.CommitAsync();
            return MapGRToDto(gr);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // Không paging
    public async Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? poId = null)
    {
        return await GetGRsAsync(poId, page: 1, pageSize: int.MaxValue);
    }

    // Có paging
    public async Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? poId = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Set<GoodsReceipt>().Include(x => x.Items).AsQueryable();
        if (poId.HasValue)
            query = query.Where(x => x.PurchaseOrderId == poId.Value);

        var grs = await query.OrderByDescending(x => x.CreatedAt)
                             .Skip((page - 1) * pageSize)
                             .Take(pageSize)
                             .ToListAsync();

        return grs.Select(MapGRToDto).ToList();
    }

    public async Task CancelGRAsync(Guid grId)
    {
        var gr = await _db.Set<GoodsReceipt>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == grId);
        if (gr == null) throw new NotFoundException("GR not found");

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            foreach (var item in gr.Items)
            {
                await _inventoryService.AdjustAsync(
                    warehouseId: gr.WarehouseId,
                    locationId: item.LocationId,
                    productId: item.ProductId,
                    qtyChange: -item.Quantity,
                    actionType: InventoryActionType.AdjustDecrease,
                    refCode: gr.Code
                );
                item.UpdatedAt = DateTime.UtcNow;
            }

            gr.UpdatedAt = DateTime.UtcNow;
            _db.Set<GoodsReceipt>().Remove(gr);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // ========================
    // Private mapping helpers
    // ========================
    private static PurchaseOrderDto MapPOToDto(PurchaseOrder po) => new()
    {
        Id = po.Id,
        Code = po.Code,
        SupplierId = po.SupplierId,
        Status = po.Status,
        CreatedAt = po.CreatedAt,
        UpdatedAt = po.UpdatedAt,
        ApprovedAt = po.ApprovedAt,
        Items = po.Items.Select(i => new PurchaseOrderItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Price = i.Price,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList()
    };

    private static GoodsReceiptDto MapGRToDto(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        Code = gr.Code,
        PurchaseOrderId = gr.PurchaseOrderId,
        WarehouseId = gr.WarehouseId,
        CreatedAt = gr.CreatedAt,
        UpdatedAt = gr.UpdatedAt,
        Items = gr.Items.Select(i => new GoodsReceiptItemDto
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            LocationId = i.LocationId,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList()
    };
}

// ========================
// Custom exceptions
// ========================
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message) { }
}
