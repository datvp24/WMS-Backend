using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Service.Sales;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Application.Services.Inventorys;
using Wms.Domain.Entity.Sales;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
using Wms.Application.DTOS.Sales;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Domain.Service.Sales
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly AppDbContext _dbContext;
        private readonly IInventoryService _inventoryService;
        private readonly IJwtService jwtService;
        private readonly IMapper _mapper;
        private readonly IWarehouseService _warehouse;

        public SalesOrderService(AppDbContext dbContext, IMapper mapper, IInventoryService _inventory, IJwtService jwt, IWarehouseService warehouse)
        {
            _inventoryService = _inventory;
            _dbContext = dbContext;
            _warehouse = warehouse;
            jwtService = jwt;
            _mapper = mapper;
        }

        #region Create / Update / Get

        public async Task<SalesOrderDto> CreateSOAsync(SalesOrderDto dto)
        {
            var so = new SalesOrder
            {
                Id = Guid.NewGuid(),  // Tạo Id mới cho SO
                Code = GenerateSOCode(),
                CustomerId = dto.CustomerId,
                Status = SOStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new SalesOrderItem
                {
                    Id = Guid.NewGuid(),  // Tạo Id mới cho mỗi Item
                    ProductId = i.ProductId,
                    SalesOrderId = dto.Id,  // ★ SỬ DỤNG so.Id (đã tạo ở trên)
                    Status = SOStatus.Pending,
                    WarehouseId = i.WarehouseId,
                    Quantity = i.OrderQty,  // Đảm bảo khớp tên field DTO
                    Issued_Qty = 0,
                    Price = i.Price,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            if (jwtService.GetUserId().HasValue)
                so.CreatedBy = jwtService.GetUserId().Value;

            _dbContext.Set<SalesOrder>().Add(so);
            await _dbContext.SaveChangesAsync();  // Lưu SO + Items cùng lúc

            // Reload để đảm bảo Items được load đầy đủ
            var savedSo = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == so.Id);

            return _mapper.Map<SalesOrderDto>(savedSo ?? so);
        }



        public async Task<SalesOrderDto> GetSOAsync(Guid soId)
        {
            var entity = await _dbContext.SalesOrders
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.GoodsIssues)
                    .ThenInclude(gi => gi.Items)
                        .ThenInclude(giItem => giItem.Product)
                .Include(x => x.Customer)
                .FirstOrDefaultAsync(x => x.Id == soId);

            if (entity == null)
                throw new Exception("SalesOrder not found");

            return _mapper.Map<SalesOrderDto>(entity);
        }

        public async Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId)
        {
            var gi = await _dbContext.Set<GoodsIssue>()
                .Where(g => g.Id == goodsIssueId)
                .Include(g => g.SalesOrder)
                .Include(g => g.Warehouse)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Product)
                .Include(g => g.Items)
                    .ThenInclude(i => i.Allocations)
                        .ThenInclude(a => a.Location) // ✅ Đảm bảo đã Include Location
                .Select(g => new GoodsIssueDetailDto
                {
                    Id = g.Id,
                    Code = g.Code,
                    SalesOrderCode = g.SalesOrder.Code,
                    WarehouseName = g.Warehouse.Name,
                    Status = (int)g.Status,
                    Items = g.Items.Select(i => new GoodsIssueItemDtoForFrontend
                    {
                        Id = i.Id,
                        ProductId = i.ProductId,
                        ProductCode = i.Product.Code,
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        PickedQty = i.Allocations.Sum(a => a.PickedQty),
                        IssuedQty = i.Issued_Qty,
                        Status = (int)i.Status,
                        Allocations = i.Allocations.Select(a => new GoodsIssueAllocate1Dto
                        {
                            Id = a.Id,
                            // ✅ QUAN TRỌNG: Bạn phải gán LocationId ở đây!
                            LocationId = a.LocationId,

                            // ✅ Lấy Code trực tiếp từ Object Location đã Include
                            LocationCode = a.Location.Code ?? "Chưa xác định",

                            AllocatedQty = a.AllocatedQty,
                            PickedQty = a.PickedQty,
                            Status = (int)a.Status
                        }).ToList()
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            return gi;
        }

        public async Task<List<SalesOrderDto>> QuerySOsAsync(SalesOrderQueryDto dto)
        {
            var query = _dbContext.SalesOrders
                .Include(x => x.Items)
                .Include(x => x.Customer)
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.Code))
                query = query.Where(x => x.Code.Contains(dto.Code));

            if (dto.CustomerId.HasValue)
                query = query.Where(x => x.CustomerId == dto.CustomerId.Value);

            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == dto.Status);

            if (dto.CreatedFrom.HasValue)
                query = query.Where(x => x.CreatedAt >= dto.CreatedFrom.Value);

            if (dto.CreatedTo.HasValue)
                query = query.Where(x => x.CreatedAt <= dto.CreatedTo.Value);

            var list = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            return _mapper.Map<List<SalesOrderDto>>(list);
        }
        public async Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto)
        {
            var query = _dbContext.GoodsIssues
                .Include(x => x.SalesOrder)
                .Include(x => x.Warehouse)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Product)
                .Include(x => x.Items)
                    .ThenInclude(i => i.Allocations).ThenInclude(s => s.Location)// ✅ Lấy danh sách phân bổ
                .AsQueryable();

            // --- Giữ nguyên các phần filter của bạn ---
            if (!string.IsNullOrEmpty(dto.Code))
                query = query.Where(x => x.Code.Contains(dto.Code));

            if (dto.SalesOrderId.HasValue)
                query = query.Where(x => x.SalesOrderId == dto.SalesOrderId.Value);

            if (dto.WarehouseId.HasValue)
                query = query.Where(x => x.WarehouseId == dto.WarehouseId.Value);

            if (dto.Status.HasValue)
                query = query.Where(x => x.Status == dto.Status.Value);

            if (dto.IssuedFrom.HasValue)
                query = query.Where(x => x.IssuedAt >= dto.IssuedFrom.Value);

            if (dto.IssuedTo.HasValue)
                query = query.Where(x => x.IssuedAt <= dto.IssuedTo.Value);
            // ------------------------------------------

            query = query.OrderByDescending(x => x.IssuedAt);

            var list = await query
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            return _mapper.Map<List<GoodsIssueDto>>(list);
        }
        #endregion

        #region Approve / Reject

        public async Task<SalesOrderDto> ApproveSOAsync(Guid soId)
        {
            var entity = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == soId);

            if (entity == null)
                throw new Exception("SalesOrder not found");

            if (entity.Status != SOStatus.Pending)
                throw new Exception("Only Pending orders can be approved");

            entity.Status = SOStatus.Approve;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.ApproveBy = jwtService.GetUserId();
            entity.ApprovedAt = DateTime.UtcNow;

            foreach (var item in entity.Items.Where(x => x.Status == SOStatus.Pending))
            {
                item.Status = SOStatus.Approve;
                item.UpdatedAt = DateTime.UtcNow;
            }

            var GroupWarehouse = entity.Items
                .Where(x => x.Status == SOStatus.Approve)
                .GroupBy(x => x.WarehouseId);

            foreach (var group in GroupWarehouse)
            {
                var warehouseId = group.Key;

                var gi = new GoodsIssue
                {
                    Id = Guid.NewGuid(),
                    SalesOrderId = entity.Id,
                    Code = GenerateGICode(),
                    WarehouseId = warehouseId,
                    Status = GIStatus.Pending,
                    CreateAt = DateTime.UtcNow,
                    Items = new List<GoodsIssueItem>()
                };

                foreach (var item in group)
                {
                    var gii = new GoodsIssueItem
                    {
                        Id = Guid.NewGuid(),
                        GoodsIssueId = gi.Id,
                        SOIId = item.Id,
                        ProductId = item.ProductId,
                        Status = GIStatus.Pending,
                        Quantity = item.Quantity,
                        Issued_Qty = item.Issued_Qty,
                        CreatedAt = DateTime.UtcNow,
                        Allocations = new List<GoodsIssueAllocate>()
                    };

                    decimal remainingQty = item.Quantity;

                    var locations = await _inventoryService.GetAvailableLocations(item.ProductId, warehouseId);

                    foreach (var loc in locations)
                    {
                        if (remainingQty <= 0) break;

                        var allocQty = Math.Min(remainingQty, loc.AvailableQty);

                        var gia = new GoodsIssueAllocate
                        {
                            Id = Guid.NewGuid(),
                            GoodsIssueItemId = gii.Id,
                            LocationId = loc.Id,
                            AllocatedQty = allocQty,
                            PickedQty = 0,
                            Status = GIAStatus.Planned
                        };

                        gii.Allocations.Add(gia);
                        remainingQty -= allocQty;
                    }

                    if (remainingQty > 0)
                    {
                        var gia = new GoodsIssueAllocate
                        {
                            Id = Guid.NewGuid(),
                            GoodsIssueItemId = gii.Id,
                            LocationId = Guid.Empty,
                            AllocatedQty = remainingQty,
                            PickedQty = 0,
                            Status = GIAStatus.Planned
                        };
                        gii.Allocations.Add(gia);
                    }

                    gi.Items.Add(gii);
                }

                _dbContext.Set<GoodsIssue>().Add(gi);
            }

            await _dbContext.SaveChangesAsync();
            return _mapper.Map<SalesOrderDto>(entity);
        }

        
        public async Task Picking(GoodsIssueItemDto dto)
        {
            var gii = await _dbContext.GoodsIssueItems.FirstOrDefaultAsync(s => s.Id == dto.Id);
            var gi = await  _dbContext.GoodsIssues.FirstOrDefaultAsync(s => s.Id == dto.GoodsIssueId);

            foreach (var item in dto.Items)
            {
                var gia = await _dbContext.goodsIssueAllocates.FirstOrDefaultAsync(s => s.Id == item.Id);
                if (gia == null)
                {
                    // Log lại hoặc ném ngoại lệ rõ ràng hơn để debug
                    throw new Exception($"Không tìm thấy bản ghi Allocate với ID: {item.Id}");
                }
                gia.PickedQty = item.PickedQty;
                var location = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                Console.WriteLine($"DEBUG: WH={gi.WarehouseId}, LOC={gia.LocationId}, PROD={dto.ProductId}, QTY={gia.PickedQty}");
                // 1. TRỪ hàng tại Kệ (Nơi lấy đi)
                await _inventoryService.AdjustAsync(
                    gi.WarehouseId,
                    item.LocationId,
                    dto.ProductId,
                    gia.PickedQty,
                    Enums.Inventory.InventoryActionType.AdjustDecrease); // Sửa thành DECREASE

                // 2. CỘNG hàng vào Cổng xuất (Nơi tập kết hàng)
                await _inventoryService.AdjustAsync(
                    gi.WarehouseId,
                    location.Id,
                    dto.ProductId,
                    gia.PickedQty,
                    Enums.Inventory.InventoryActionType.AdjustIncrease); // Sửa thành INCREASE
                if (gia.PickedQty < item.AllocatedQty)
                {
                    decimal remainingQty = gia.AllocatedQty - gia.PickedQty;

                    var locations = await _inventoryService.GetAvailableLocations(gii.ProductId, gi.WarehouseId);

                    foreach (var loc in locations)
                    {
                        if (remainingQty <= 0) break;

                        var allocQty = Math.Min(remainingQty, loc.AvailableQty);

                        var gia1 = new GoodsIssueAllocate
                        {
                            Id = Guid.NewGuid(),
                            GoodsIssueItemId = gii.Id,
                            LocationId = loc.Id,
                            AllocatedQty = allocQty,
                            PickedQty = 0,
                            Status = GIAStatus.Planned
                        };

                        gii.Allocations.Add(gia1);
                        remainingQty -= allocQty;
                    }

                    if (remainingQty > 0)
                    {
                        var gia2 = new GoodsIssueAllocate
                        {
                            Id = Guid.NewGuid(),
                            GoodsIssueItemId = gii.Id,
                            LocationId = Guid.Empty,
                            AllocatedQty = remainingQty,
                            PickedQty = 0,
                            Status = GIAStatus.Planned
                        };
                        gii.Allocations.Add(gia2);
                    }

                }
                gia.Status = GIAStatus.Picked;

            }
        }
        public async Task OutgoingStockCount(IssueGoodsDto dto)
        {
            using var tx = await _dbContext.Database.BeginTransactionAsync();

            var gii = await _dbContext.GoodsIssueItems
                .FirstOrDefaultAsync(x => x.Id == dto.GoodsIssueItemId);

            if (gii == null)
                throw new Exception("GoodsIssueItem not found");

            if (dto.IssuedQty <= 0)
                throw new Exception("Issued quantity must be greater than zero");

            if (gii.Issued_Qty + dto.IssuedQty > gii.Quantity)
                throw new Exception("Issued quantity exceeds required quantity");

            var totalPicked = await _dbContext.goodsIssueAllocates
                .Where(x => x.GoodsIssueItemId == gii.Id)
                .SumAsync(x => x.PickedQty);

            if (dto.IssuedQty > totalPicked - gii.Issued_Qty)
                throw new Exception("Issued quantity exceeds picked quantity");

            gii.Issued_Qty += dto.IssuedQty;

            gii.Status = gii.Issued_Qty >= gii.Quantity
                ? GIStatus.Complete
                : GIStatus.Partically_Issued;

        var gi = await _dbContext.GoodsIssues
                .FirstOrDefaultAsync(x => x.Id == gii.GoodsIssueId);

            var issueLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
            if (issueLocation == null) throw new Exception("location null");


            await _inventoryService.AdjustAsync(
                gi.WarehouseId,
                issueLocation.Id,
                gii.ProductId,
                gii.Issued_Qty,
                InventoryActionType.Issue
            );

            var giItems = await _dbContext.GoodsIssueItems
                .Where(x => x.GoodsIssueId == gi.Id)
                .ToListAsync();

            gi.Status = giItems.All(i => i.Status == GIStatus.Complete)
                ? GIStatus.Complete
                : GIStatus.Partically_Issued;

            var soi = await _dbContext.SalesOrderItems
                .FirstOrDefaultAsync(s => s.Id == gii.SOIId);

            if (soi != null)
            {
                soi.Issued_Qty += dto.IssuedQty;
                soi.Status = soi.Issued_Qty >= soi.Quantity
                    ? SOStatus.Complete
                    : SOStatus.Partically_Issued;
            }
            var so = await _dbContext.SalesOrders.FirstOrDefaultAsync(s => s.Id == soi.SalesOrderId);
            var listsoi = _dbContext.SalesOrderItems.Where(s => s.SalesOrderId == so.Id).All(i => i.Status == SOStatus.Complete);
            so.Status = listsoi ? SOStatus.Complete : SOStatus.Partically_Issued;

            gi.IssuedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
        }


        private string GenerateGICode()
        {
            // Lấy ngày hôm nay
            var today = DateTime.UtcNow.Date; // chỉ YYYY-MM-DD

            // Đếm số GR đã tạo trong ngày hôm nay
            var countToday = _dbContext.GoodsIssues
                                .Count(gr => gr.CreateAt >= today && gr.CreateAt < today.AddDays(1));

            // Tăng số thứ tự 1
            var seq = countToday + 1;

            // Format code: GR-YYYYMMDD-XXXX
            var code = $"GI-{today:yyyyMMdd}-{seq:0000}";

            return code;
        }
        private string GenerateSOCode()
        {
            // Lấy ngày hôm nay
            var today = DateTime.UtcNow.Date; // chỉ YYYY-MM-DD

            // Đếm số GR đã tạo trong ngày hôm nay
            var countToday = _dbContext.SalesOrders
                                .Count(gr => gr.CreatedAt >= today && gr.CreatedAt < today.AddDays(1));

            // Tăng số thứ tự 1
            var seq = countToday + 1;

            // Format code: GR-YYYYMMDD-XXXX
            var code = $"SO-{today:yyyyMMdd}-{seq:0000}";

            return code;
        }

        public async Task<SalesOrderDto> RejectSOAsync(Guid soId)
        {
            var entity = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == soId);

            if (entity == null)
                throw new Exception("SalesOrder not found");

            if (entity.Status != SOStatus.Pending)
                throw new Exception("Only DRAFT orders can be rejected");

            entity.Status = SOStatus.Rejected;
            entity.UpdatedAt = DateTime.UtcNow;
            foreach (var item in entity.Items)
            {
                item.Status = SOStatus.Rejected;
            }

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SalesOrderDto>(entity);
        }

        #endregion
    }
}
