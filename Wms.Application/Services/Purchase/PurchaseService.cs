using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using Wms.Application.DTOS.Purchase;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Purchase;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Purchase;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Infrastructure.Persistence.Context;
using Wms.Domain.Enums.Purchase;

namespace Wms.Application.Services.Purchase;

public class PurchaseService : IPurchaseService
{
    private readonly AppDbContext _db;
    private readonly IInventoryService _inventoryService;   
    private readonly IJwtService _jwt;
    private readonly IWarehouseService _locationService;

    public PurchaseService(AppDbContext db, IInventoryService inventoryService, IJwtService jwt, IWarehouseService warehouseService)
    {
        _db = db;
        _locationService = warehouseService;
        _jwt = jwt;
        _inventoryService = inventoryService;
    }

    // ========================
    // PURCHASE ORDER
    // ========================
    private string GenerateGRCode()
    {
        // Lấy ngày hôm nay
        var today = DateTime.UtcNow.Date; // chỉ YYYY-MM-DD

        // Đếm số GR đã tạo trong ngày hôm nay
        var countToday = _db.GoodsReceipts
                            .Count(gr => gr.CreatedAt >= today && gr.CreatedAt < today.AddDays(1));

        // Tăng số thứ tự 1
        var seq = countToday + 1;

        // Format code: GR-YYYYMMDD-XXXX
        var code = $"GR-{today:yyyyMMdd}-{seq:0000}";

        return code;
    }

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
                WarehouseId = i.WarehouseId,
                Quantity = i.Quantity,
                Status = Status.Pending,
                Received_qty = 0,
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
        // 1. Lấy PO
        var po = await _db.PurchaseOrders
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);

        if (po == null) throw new NotFoundException("PO not found");

        if (po.Status != "Pending")
            throw new InvalidOperationException("Only Pending PO can be approved");

        // 2. Approve PO + POI
        po.Status = "Approve";
        po.ApprovedAt = DateTime.UtcNow;
        po.ApprovedBy = _jwt.GetUserId();
        po.UpdatedAt = DateTime.UtcNow;

        foreach (var poi in po.Items)
        {
            if (poi.Status == Status.Pending)
            {
                poi.Status = Status.Approve;
                poi.UpdatedAt = DateTime.UtcNow;
            }
        }

        // 3. Group POI theo Warehouse để tạo GR
        var groupedPOI = po.Items
                           .Where(i => i.Status == Status.Approve)
                           .GroupBy(i => i.WarehouseId);


        foreach (var group in groupedPOI)
        {
            var warehouseId = group.Key;

            var gr = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                PurchaseOrderId = po.Id,
                WarehouseId = warehouseId,
                Code = GenerateGRCode(), // custom function
                Status = Status.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = new List<GoodsReceiptItem>()
            };

            foreach (var poi in group)
            {
                var gri = new GoodsReceiptItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = poi.ProductId,
                    GoodsReceiptId = gr.Id,
                    Quantity = poi.Quantity,
                    POIid = poi.Id,
                    Received_Qty = 0,
                    CreatedAt = DateTime.UtcNow
                };
                gr.Items.Add(gri);
            }

            _db.GoodsReceipts.Add(gr);
        }

        // 4. Save all
        await _db.SaveChangesAsync();

        return MapPOToDto(po);
    }


    public async Task<PurchaseOrderDto> RejectPOAsync(Guid poId)
    {
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);

        if (po == null) throw new NotFoundException("PO not found");

        if (po.Status != "Pending")
            throw new InvalidOperationException("Only Pending PO can be rejected");

        po.Status = "Rejected";
        po.UpdatedAt = DateTime.UtcNow;

        foreach (var item in po.Items)
        {
            if (item.Status == Status.Pending) // chỉ reject POI chưa approved
            {
                item.Status = Status.Rejected;
                item.UpdatedAt = DateTime.UtcNow;
            }
        }

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
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList()
            };

            _db.Set<GoodsReceipt>().Add(gr);
            await _db.SaveChangesAsync();

            // Update inventory
            foreach (var item in gr.Items)
            {
                await _inventoryService.Adjust1Async(
                    warehouseId: gr.WarehouseId,
                    productId: item.ProductId,
                    qtyChange: item.Quantity,
                    locationId: null,
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

    public async Task IncomingStockCount(GoodsReceiptItem1Dto dto)
    {
        var item = await _db.GoodsReceiptItems
            .FirstOrDefaultAsync(s => s.Id == dto.Id);
        if (item == null) throw new Exception("Dòng hàng nhập kho không tồn tại (ID null hoặc sai)");

        item.Received_Qty += dto.Received_Qty;

        if (item.Received_Qty >= item.Quantity)
            item.Status = Domain.Enums.Purchase.GRIStatus.Complete;
        else if (item.Received_Qty > 0)
            item.Status = Domain.Enums.Purchase.GRIStatus.Partial;

        var poi = await _db.PurchaseOrderItems.FirstOrDefaultAsync(p => p.Id == item.POIid);
        if (poi != null)
        {
            poi.Received_qty += dto.Received_Qty;
            if (poi.Received_qty >= item.Quantity)
                poi.Status = Status.Complete;
            else
            {
                poi.Status = Status.Partically_Received;
            }

                var locationId = await _locationService.GetReceivingLocationId(poi.WarehouseId);
            await _inventoryService.AdjustAsync(
                poi.WarehouseId,
                locationId,
                item.ProductId,
                dto.Received_Qty,
                InventoryActionType.Receive
            );
        }

        var gr = await _db.GoodsReceipts
            .Include(p => p.Items)
            .FirstOrDefaultAsync(s => s.Id == item.GoodsReceiptId);

        if (gr != null)
        {
            if (gr.Items.All(i => i.Status == Domain.Enums.Purchase.GRIStatus.Complete))
                gr.Status = Status.Complete;
            else
                gr.Status = Status.Partically_Received; 

            var po = await _db.PurchaseOrders
                .Include(s => s.Items)
                .FirstOrDefaultAsync(s => s.Id == gr.PurchaseOrderId);

            if (po != null)
            {
                if (po.Items.All(x => x.Received_qty >= x.Quantity))
                    po.Status = Status.Complete.ToString();
                else if (po.Items.Any(x => x.Received_qty > 0))
                    po.Status = Status.Partically_Received.ToString();
            }
        }

        await _db.SaveChangesAsync();
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
    private async Task<PurchaseOrderDto> MapPOWithReceivedQtyAsync(PurchaseOrder po)
    {
        // Lấy tổng số lượng đã nhận từ các phiếu GR (GoodsReceipt) liên quan đến PO này
        var receivedQtys = await _db.Set<GoodsReceiptItem>()
            .Where(gri => gri.GoodsReceipt.PurchaseOrderId == po.Id)
            .GroupBy(gri => gri.ProductId)
            .Select(g => new {
                ProductId = g.Key,
                TotalReceived = g.Sum(x => x.Quantity)
            })
            .ToListAsync();

        return new PurchaseOrderDto
        {
            Id = po.Id,
            Code = po.Code,
            SupplierId = po.SupplierId,
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            UpdatedAt = po.UpdatedAt,
            ApprovedAt = po.ApprovedAt,
            Items = po.Items.Select(i => {
                var received = receivedQtys.FirstOrDefault(r => r.ProductId == i.ProductId)?.TotalReceived ?? 0;
                return new PurchaseOrderItemDto
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity, // Đây là số lượng đặt (Ordered Qty)
                    ReceivedQuantity = received, // Số lượng đã về kho (CẦN THÊM PROPERTY NÀY VÀO DTO)
                    Price = i.Price,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                };
            }).ToList()
        };
    }
    public async Task<PurchaseOrderDto> GetPOM0Async(Guid poId)
    {
        // 1. Lấy thông tin PO và Items
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items.Where(i => i.Quantity !=0))
                          .FirstOrDefaultAsync(x => x.Id == poId);
        
        if (po == null) throw new NotFoundException("PO not found");

        // 2. Tính tổng số lượng đã nhận từ tất cả các GoodsReceipt liên quan đến PO này
        // Truy vấn vào bảng GoodsReceiptItem (nơi lưu thực tế số lượng đã nhập)
        var receivedQtys = await _db.Set<GoodsReceiptItem>()
            .Where(x => x.GoodsReceipt.PurchaseOrderId == poId)
            .GroupBy(x => x.ProductId)
            .Select(g => new {
                ProductId = g.Key,
                Total = g.Sum(i => i.Quantity)

            })
            .ToListAsync();

        // 3. Map sang DTO và gán con số thực tế vào
        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            Code = po.Code,
            SupplierId = po.SupplierId,
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity, // Đây là số lượng đặt (ví dụ 112)
                Price = i.Price,
                // Gán số lượng đã nhận từ kết quả GroupBy ở trên
                ReceivedQuantity = receivedQtys.FirstOrDefault(r => r.ProductId == i.ProductId)?.Total ?? 0
            }).ToList()
        };

        return dto;
    }
    public async Task<PurchaseOrderDto> GetPOAsync(Guid poId)
    {
        // 1. Lấy thông tin PO và Items
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);

        if (po == null) throw new NotFoundException("PO not found");

        // 2. Tính tổng số lượng đã nhận từ tất cả các GoodsReceipt liên quan đến PO này
        // Truy vấn vào bảng GoodsReceiptItem (nơi lưu thực tế số lượng đã nhập)
        var receivedQtys = await _db.Set<GoodsReceiptItem>()
            .Where(x => x.GoodsReceipt.PurchaseOrderId == poId)
            .GroupBy(x => x.ProductId)
            .Select(g => new {
                ProductId = g.Key,
                Total = g.Sum(i => i.Quantity)
            })
            .ToListAsync();

        // 3. Map sang DTO và gán con số thực tế vào
        var dto = new PurchaseOrderDto
        {
            Id = po.Id,
            Code = po.Code,
            SupplierId = po.SupplierId,
            Status = po.Status,
            CreatedAt = po.CreatedAt,
            Items = po.Items.Select(i => new PurchaseOrderItemDto
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity, // Đây là số lượng đặt (ví dụ 112)
                Price = i.Price,
                // Gán số lượng đã nhận từ kết quả GroupBy ở trên
                ReceivedQuantity = receivedQtys.FirstOrDefault(r => r.ProductId == i.ProductId)?.Total ?? 0
            }).ToList()
        };

        return dto;
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
                await _inventoryService.Adjust1Async(
                    warehouseId: gr.WarehouseId,
                    locationId: null,
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
            ReceivedQuantity = i.Received_qty,
            Status = i.Status,
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
        Status = gr.Status,
        CreatedAt = gr.CreatedAt,
        UpdatedAt = gr.UpdatedAt,
        Items = gr.Items.Select(i => new GoodsReceiptItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            Received_Qty = i.Received_Qty,
            Status = i.Status,
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
