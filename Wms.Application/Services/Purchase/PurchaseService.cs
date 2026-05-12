using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using Wms.Application.DTOS.Purchase;
using Wms.Application.DTOS.StockTake;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Purchase;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Purchase;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Domain.Enums.Purchase;
using Wms.Infrastructure.Persistence.Context;

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
    // PRIVATE HELPERS
    // ========================

    /// <summary>
    /// Tự động sinh mã PO theo format: PO-YYYYMMDD-0001
    /// </summary>
    private string GeneratePOCode()
    {
        var today = DateTime.UtcNow.Date;

        var countToday = _db.PurchaseOrders
            .Count(po => po.CreatedAt >= today && po.CreatedAt < today.AddDays(1));

        var seq = countToday + 1;

        return $"PO-{today:yyyyMMdd}-{seq:0000}";
    }

    /// <summary>
    /// Tự động sinh mã GR theo format: GR-YYYYMMDD-0001
    /// </summary>
    private string GenerateGRCode()
    {
        var today = DateTime.UtcNow.Date;

        var countToday = _db.GoodsReceipts
            .Count(gr => gr.CreatedAt >= today && gr.CreatedAt < today.AddDays(1));

        var seq = countToday + 1;

        return $"GR-{today:yyyyMMdd}-{seq:0000}";
    }

    /// <summary>
    /// Tự động sinh LotCode từ ProductId và ngày + giờ nhập.
    /// Format: LOT-{ProductId 6 chữ số}-{YYYYMMDD}-{HHmmss}
    /// Ví dụ: LOT-000042-20260308-143022
    /// Timestamp giây đảm bảo unique cho mỗi lần nhập trong ngày.
    /// </summary>
    private static string GenerateLotCode(int productId, DateTime date)
    {
        var productShort = productId.ToString("D6");
        var timestamp = date.ToString("HHmmss");
        return $"LOT-{productShort}-{date:yyyyMMdd}-{timestamp}";
    }

    // ========================
    // PURCHASE ORDER
    // ========================

    public async Task<PurchaseOrderDto> CreatePOAsync(PurchaseOrderDto dto)
    {
        foreach (var item in dto.Items)
        {
            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(s => s.Id == item.WarehouseId);

            if (warehouse == null)
                throw new BusinessException(
                    "WAREHOUSE_NOT_FOUND",
                    "Kho nhận không tồn tại"
                );

            if (warehouse.WarehouseType != WarehouseType.RawMaterial)
                throw new BusinessException(
                    "INVALID_WAREHOUSE_TYPE",
                    $"Kho \"{warehouse.Name}\" không phải kho nguyên vật liệu, không thể nhập hàng"
                );
        }

        var po = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            Code = GeneratePOCode(),   // ← tự sinh, bỏ dto.Code
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
        var po = await _db.PurchaseOrders
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);

        if (po == null) throw new NotFoundException("PO not found");

        if (po.Status != "Pending")
            throw new InvalidOperationException("Only Pending PO can be approved");

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
                Code = GenerateGRCode(),
                Status = Status.Pending,
                ReceiptType = ReceiptType.Purchase,
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
            if (item.Status == Status.Pending)
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

        var warehousecheck = _db.Warehouses.FirstOrDefault(s => s.Id == dto.WarehouseId);
        //if (warehousecheck.WarehouseType != WarehouseType.FinishedGoods)
        //    throw new Exception("Chỉ có thể nhập kho thành phẩm");

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var gr = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                Code = GenerateGRCode(),   // ← tự sinh
                PurchaseOrderId = dto.PurchaseOrderId,
                ReceiptType = ReceiptType.Production,
                WarehouseId = dto.WarehouseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Productions = dto.ProductionReceiptItems.Select(i => new ProductionReceiptItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Receipt_Qty = i.Receipt_Qty,
                    Status = GRIStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList()
            };

            _db.GoodsReceipts.Add(gr);
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapProductionGRToDto(gr);
        });
    }

    public async Task<GoodsReceiptDto> ApproveProductionReceipt(GoodsReceiptDto dto)
    {
        var gr = await _db.GoodsReceipts.FirstOrDefaultAsync(s => s.Id == dto.Id);
        if (gr == null)
            throw new Exception("Đơn nhập không tồn tại");
        if (gr.Status == Status.Approve)
            throw new Exception("Chỉ có thể chấp thuận (Approve) đơn nhập có trạng thái là đang xử lý (Pending)");

        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();
            gr.Status = Status.Approve;
            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapProductionGRToDto(gr);
        });
    }

    public async Task<List<GoodsReceipt>> getGRbytype(GRByTypeDto dto)
    {
        var GRlist = _db.GoodsReceipts
            .Include(s => s.Items)
            .Include(s => s.Productions)
            .Where(s => s.ReceiptType == dto.ReceiptType)
            .ToList();
        return GRlist;
    }

    public async Task<GoodsReceiptDto> CountingReceiptProduction(GoodsReceiptDto dto)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            var gr = await _db.GoodsReceipts
                .Include(s => s.Productions)
                .FirstOrDefaultAsync(s => s.Id == dto.Id);

            if (gr == null)
                throw new Exception("Mã đơn nhập không tồn tại");

            if (gr.Status != Status.Approve && gr.Status != Status.Partically_Received)
                throw new Exception("Đơn nhập không hợp lệ để kiểm đếm");

            var location = await _locationService.GetReceivingLocationId(dto.WarehouseId);
            var receivedAt = DateTime.UtcNow;

            foreach (var item in dto.ProductionReceiptItems)
            {
                if (item.Receipt_Qty <= 0)
                    continue;

                var production = gr.Productions.FirstOrDefault(s => s.Id == item.Id);
                if (production == null)
                    throw new Exception("Chi tiết sản phẩm nhập không tồn tại");

                if (production.Receipt_Qty + item.Receipt_Qty > production.Quantity)
                    throw new Exception("Số lượng nhận vượt quá số lượng yêu cầu");

                production.Receipt_Qty += item.Receipt_Qty;

                production.Status = production.Receipt_Qty == production.Quantity
                    ? GRIStatus.Complete
                    : GRIStatus.Partial;

                await _inventoryService.AdjustAsync(
                    dto.WarehouseId,
                    location,
                    item.ProductId,
                    item.Receipt_Qty,
                    InventoryActionType.Receive,
                    refCode: gr.Code,
                    lotCode: GenerateLotCode(item.ProductId, receivedAt),
                    expiryDate: item.ExpiryDate,
                    manufacturingDate: item.ManufacturingDate
                );
            }

            if (gr.Productions.All(s => s.Status == GRIStatus.Complete))
                gr.Status = Status.Complete;
            else if (gr.Productions.Any(s => s.Status != GRIStatus.Pending))
                gr.Status = Status.Partically_Received;

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return MapProductionGRToDto(gr);
        });
    }

    // Không paging
    public async Task<List<GoodsReceiptDto>> GetGRsAsync(Guid? poId = null)
    {
        return await GetGRsAsync(poId, page: 1, pageSize: int.MaxValue);
    }

    public async Task IncomingStockCount(GoodsReceiptItem1Dto dto)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var item = await _db.GoodsReceiptItems
                    .FirstOrDefaultAsync(s => s.Id == dto.Id);

                var product = await _db.Products.FirstOrDefaultAsync(s => s.Id == dto.ProductId);
                if (product == null)
                    throw new Exception("Chỉ nhập kho những sản phẩm thuộc loại nguyên vật liệu");

                if (item == null)
                    throw new Exception("Dòng hàng nhập kho không tồn tại (ID null hoặc sai)");

                // 1️⃣ Update GR Item
                item.Received_Qty += dto.Received_Qty;

                if (item.Received_Qty >= item.Quantity)
                    item.Status = GRIStatus.Complete;
                else if (item.Received_Qty > 0)
                    item.Status = GRIStatus.Partial;

                // 2️⃣ Update PO Item + Inventory
                var poi = await _db.PurchaseOrderItems
                    .FirstOrDefaultAsync(p => p.Id == item.POIid);
                var gr = await _db.GoodsReceipts
                            .Include(p => p.Items)
                            .FirstOrDefaultAsync(s => s.Id == item.GoodsReceiptId);

                if (poi != null)
                {
                    poi.Received_qty += dto.Received_Qty;

                    if (poi.Received_qty >= item.Quantity)
                        poi.Status = Status.Complete;
                    else
                        poi.Status = Status.Partically_Received;

                    var locationId = await _locationService
                        .GetReceivingLocationId(poi.WarehouseId);

                    await _inventoryService.AdjustAsync(
                        poi.WarehouseId,
                        locationId,
                        item.ProductId,
                        dto.Received_Qty,
                        InventoryActionType.Receive,
                        refCode: gr.Code,
                        lotCode: GenerateLotCode(item.ProductId, DateTime.UtcNow),
                        expiryDate: dto.ExpiryDate,
                        manufacturingDate: dto.ManufacturingDate
                    );
                }

                // 3️⃣ Update GR status
                if (gr != null)
                {
                    gr.Status = gr.Items.All(i => i.Status == GRIStatus.Complete)
                        ? Status.Complete
                        : Status.Partically_Received;

                    // 4️⃣ Update PO status
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
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    // Có paging
    public async Task<List<GoodsReceiptDto>> GetGRsAsync(
        Guid? poId = null,
        int page = 1,
        int pageSize = 20)
    {
        var query = _db.Set<GoodsReceipt>()
            .Include(x => x.Items)
            .Include(x => x.Productions)
            .AsQueryable();

        if (poId.HasValue)
            query = query.Where(x => x.PurchaseOrderId == poId.Value);

        var grs = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return grs.Select(MapGRToDto).ToList();
    }

    public async Task<PurchaseOrderDto> GetPOM0Async(Guid poId)
    {
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items.Where(i => i.Quantity != 0))
                          .FirstOrDefaultAsync(x => x.Id == poId);

        if (po == null) throw new NotFoundException("PO not found");

        var receivedQtys = await _db.Set<GoodsReceiptItem>()
            .Where(x => x.GoodsReceipt.PurchaseOrderId == poId)
            .GroupBy(x => x.ProductId)
            .Select(g => new {
                ProductId = g.Key,
                Total = g.Sum(i => i.Quantity)
            })
            .ToListAsync();

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
                Quantity = i.Quantity,
                Price = i.Price,
                ReceivedQuantity = receivedQtys.FirstOrDefault(r => r.ProductId == i.ProductId)?.Total ?? 0
            }).ToList()
        };

        return dto;
    }

    public async Task<PurchaseOrderDto> GetPOAsync(Guid poId)
    {
        var po = await _db.Set<PurchaseOrder>()
                          .Include(x => x.Items)
                          .FirstOrDefaultAsync(x => x.Id == poId);

        if (po == null) throw new NotFoundException("PO not found");

        var receivedQtys = await _db.Set<GoodsReceiptItem>()
            .Where(x => x.GoodsReceipt.PurchaseOrderId == poId)
            .GroupBy(x => x.ProductId)
            .Select(g => new {
                ProductId = g.Key,
                Total = g.Sum(i => i.Quantity)
            })
            .ToListAsync();

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
                Quantity = i.Quantity,
                Price = i.Price,
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

    private static GoodsReceiptDto MapGRToDto(GoodsReceipt gr)
    {
        return gr.ReceiptType switch
        {
            ReceiptType.Purchase => MapPurchaseGRToDto(gr),
            ReceiptType.Production => MapProductionGRToDto(gr),
            _ => throw new Exception($"ReceiptType không hợp lệ: {gr.ReceiptType}")
        };
    }

    private static GoodsReceiptDto MapProductionGRToDto(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        Code = gr.Code,
        PurchaseOrderId = gr.PurchaseOrderId,
        WarehouseId = gr.WarehouseId,
        ReceiptType = gr.ReceiptType,
        Status = gr.Status,
        CreatedAt = gr.CreatedAt,
        UpdatedAt = gr.UpdatedAt,

        ProductionReceiptItems = gr.Productions.Select(p => new ProductionReceiptItemDto
        {
            Id = p.Id,
            GoodsReceiptId = p.GoodsReceiptId,
            ProductId = p.ProductId,
            Quantity = p.Quantity,
            Receipt_Qty = p.Receipt_Qty,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList()
    };
    public async Task<ScanReceiveResultDto> ScanPOInfoAsync(string poCode)
    {
        var po = await _db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Code == poCode.Trim().ToUpper());

        if (po == null)
            throw new NotFoundException($"Không tìm thấy đơn hàng: {poCode}");

        if (po.Status == "Rejected")
            throw new BusinessException("PO_REJECTED", "Đơn hàng đã bị từ chối");

        if (po.Status == "Complete")
            throw new BusinessException("PO_COMPLETED", "Đơn hàng đã hoàn thành");

        // Lấy GR hiện có (nếu đã approve trước đó)
        var existingGRs = await _db.GoodsReceipts
            .Include(x => x.Items)
            .Where(x => x.PurchaseOrderId == po.Id
                     && x.ReceiptType == ReceiptType.Purchase
                     && x.Status != Status.Complete)
            .ToListAsync();

        return new ScanReceiveResultDto
        {
            PO = MapPOToDto(po),
            GoodsReceipts = existingGRs.Select(MapPurchaseGRToDto).ToList(),
            NeedsApproval = po.Status == "Pending"
        };
    }

    /// <summary>
    /// BƯỚC 2 — Confirm: User đã xác nhận → tự động Approve PO + tạo GR nếu cần.
    /// POST /api/purchase/scan/{poCode}/confirm
    /// </summary>
    public async Task<ScanReceiveResultDto> ConfirmAndReceiveAsync(string poCode)
    {
        var po = await _db.PurchaseOrders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Code == poCode.Trim().ToUpper());

        if (po == null)
            throw new NotFoundException($"Không tìm thấy đơn hàng: {poCode}");

        if (po.Status == "Rejected")
            throw new BusinessException("PO_REJECTED", "Đơn hàng đã bị từ chối");

        if (po.Status == "Complete")
            throw new BusinessException("PO_COMPLETED", "Đơn hàng đã hoàn thành");

        // Chỉ approve nếu còn Pending
        if (po.Status == "Pending")
        {
            po.Status = "Approve";
            po.ApprovedAt = DateTime.UtcNow;
            po.ApprovedBy = _jwt.GetUserId();
            po.UpdatedAt = DateTime.UtcNow;

            foreach (var poi in po.Items.Where(i => i.Status == Status.Pending))
            {
                poi.Status = Status.Approve;
                poi.UpdatedAt = DateTime.UtcNow;
            }

            // Tạo GR theo từng kho (giống ApprovePOAsync)
            var grouped = po.Items
                .Where(i => i.Status == Status.Approve)
                .GroupBy(i => i.WarehouseId);

            foreach (var group in grouped)
            {
                var gr = new GoodsReceipt
                {
                    Id = Guid.NewGuid(),
                    PurchaseOrderId = po.Id,
                    WarehouseId = group.Key,
                    Code = GenerateGRCode(),
                    Status = Status.Pending,
                    ReceiptType = ReceiptType.Purchase,
                    CreatedAt = DateTime.UtcNow,
                    Items = group.Select(poi => new GoodsReceiptItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = poi.ProductId,
                        Quantity = poi.Quantity,
                        POIid = poi.Id,
                        Received_Qty = 0,
                        CreatedAt = DateTime.UtcNow
                    }).ToList()
                };
                _db.GoodsReceipts.Add(gr);
            }

            await _db.SaveChangesAsync();
        }

        // Lấy tất cả GR chưa hoàn thành
        var activeGRs = await _db.GoodsReceipts
            .Include(x => x.Items)
            .Where(x => x.PurchaseOrderId == po.Id
                     && x.ReceiptType == ReceiptType.Purchase
                     && x.Status != Status.Complete)
            .ToListAsync();

        if (!activeGRs.Any())
            throw new BusinessException("ALL_GR_COMPLETED", "Tất cả phiếu nhập đã hoàn thành");

        return new ScanReceiveResultDto
        {
            PO = MapPOToDto(po),
            GoodsReceipts = activeGRs.Select(MapPurchaseGRToDto).ToList(),
            NeedsApproval = false
        };
    }

    public async Task UpdateGRStatusAsync(Guid grId, Status status)
    {
        var gr = await _db.GoodsReceipts
            .FirstOrDefaultAsync(x => x.Id == grId);

        if (gr == null)
            throw new NotFoundException("GoodsReceipt không tồn tại");

        gr.Status = status;
        gr.UpdatedAt = DateTime.UtcNow;

        // Đồng bộ các item con
        var items = await _db.GoodsReceiptItems
            .Where(x => x.GoodsReceiptId == grId)
            .ToListAsync();

        foreach (var item in items)
        {
            item.UpdatedAt = DateTime.UtcNow;
            // Nếu muốn set status item theo GRIStatus, map tương ứng ở đây
        }

        await _db.SaveChangesAsync();
    }
    private static GoodsReceiptDto MapPurchaseGRToDto(GoodsReceipt gr) => new()
    {
        Id = gr.Id,
        Code = gr.Code,
        PurchaseOrderId = gr.PurchaseOrderId,
        WarehouseId = gr.WarehouseId,
        ReceiptType = gr.ReceiptType,
        Status = gr.Status,
        CreatedAt = gr.CreatedAt,
        UpdatedAt = gr.UpdatedAt,

        Items = gr.Items == null
            ? new List<GoodsReceiptItemDto>()
            : gr.Items.Select(i => new GoodsReceiptItemDto
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
    public async Task<ScanReceiveResultDto> ScanAndProcessAsync(ScanQRPayloadDto payload)
    {
        // ── Validate warehouse ──────────────────────────────────────────
        foreach (var item in payload.Items)
        {
            var warehouse = await _db.Warehouses
                .FirstOrDefaultAsync(w => w.Id == item.WarehouseId);

            if (warehouse == null)
                throw new BusinessException("WAREHOUSE_NOT_FOUND", "Kho nhận không tồn tại");

            if (warehouse.WarehouseType != WarehouseType.RawMaterial)
                throw new BusinessException(
                    "INVALID_WAREHOUSE_TYPE",
                    $"Kho \"{warehouse.Name}\" không phải kho nguyên vật liệu"
                );
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                // ── BƯỚC 1: Tạo PO ────────────────────────────────────────
                var po = new PurchaseOrder
                {
                    Id = Guid.NewGuid(),
                    Code = GeneratePOCode(),
                    SupplierId = payload.SupplierId,
                    CreateBy = _jwt.GetUserId(),
                    Status = "Approve",                  // Tạo thẳng Approve
                    ApprovedAt = DateTime.UtcNow,
                    ApprovedBy = _jwt.GetUserId(),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Items = payload.Items.Select(i => new PurchaseOrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = i.ProductId,
                        WarehouseId = i.WarehouseId,
                        Quantity = i.Quantity,
                        Price = i.Price,
                        Status = Status.Approve,          // Approve luôn
                        Received_qty = 0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    }).ToList()
                };

                _db.PurchaseOrders.Add(po);

                // ── BƯỚC 2: Tạo GR theo từng kho ─────────────────────────
                var groupedItems = po.Items.GroupBy(i => i.WarehouseId);
                var createdGRs = new List<GoodsReceipt>();

                foreach (var group in groupedItems)
                {
                    var gr = new GoodsReceipt
                    {
                        Id = Guid.NewGuid(),
                        PurchaseOrderId = po.Id,
                        WarehouseId = group.Key,
                        Code = GenerateGRCode(),
                        Status = Status.Pending,
                        ReceiptType = ReceiptType.Purchase,
                        CreatedAt = DateTime.UtcNow,
                        Items = group.Select(poi => new GoodsReceiptItem
                        {
                            Id = Guid.NewGuid(),
                            ProductId = poi.ProductId,
                            GoodsReceiptId = Guid.Empty,  // EF set
                            Quantity = poi.Quantity,
                            POIid = poi.Id,
                            Received_Qty = 0,
                            CreatedAt = DateTime.UtcNow
                        }).ToList()
                    };

                    _db.GoodsReceipts.Add(gr);
                    createdGRs.Add(gr);
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new ScanReceiveResultDto
                {
                    PO = MapPOToDto(po),
                    GoodsReceipts = createdGRs.Select(MapPurchaseGRToDto).ToList(),
                    NeedsApproval = false
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

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