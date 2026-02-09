using AutoMapper;
using AutoMapper.Configuration.Annotations;
using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Sales;
using Wms.Application.DTOS.Sales;
using Wms.Application.Exceptions;
using Wms.Application.Interfaces.Service.Sales;
using Wms.Application.Interfaces.Services;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Warehouse;
using Wms.Application.Services.Inventorys;
using Wms.Domain.Entity.Inventorys;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Sales;
using Wms.Domain.Entity.Warehouses;
using Wms.Domain.Enums.Inventory;
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
            foreach (var item in dto.Items)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == item.ProductId);

                if (product == null)
                    throw new BusinessException(
                        "PRODUCT_NOT_FOUND",
                        $"Sản phẩm \"{item.ProductId}\" không tồn tại"
                    );

                if (product.Type != ProductType.Production)
                    throw new BusinessException(
                        "INVALID_PRODUCT_TYPE",
                        $"Sản phẩm \"{product.Name}\" không phải là thành phẩm nên không thể bán"
                    );
            }

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
                    Type = g.Type,
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
                    .ThenInclude(i => i.Allocations).ThenInclude(s => s.Location)
                .AsNoTracking() // Thêm cái này để query nhanh hơn và tránh cache dữ liệu cũ
                .AsQueryable();

            // --- Giữ nguyên logic filter của bạn ---
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

            // Sắp xếp
            query = query.OrderByDescending(x => x.IssuedAt);

            // Thực thi lấy dữ liệu từ DB
            var entities = await query
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            // Bước 1: Dùng Mapper cũ để map các trường thông thường (Items, Product, v.v.)
            var resultList = _mapper.Map<List<GoodsIssueDto>>(entities);

            // Bước 2: ÉP DỮ LIỆU trường IssuedAt (Đảm bảo không bị mất)
            for (int i = 0; i < entities.Count; i++)
            {
                // Gán trực tiếp giá trị từ Entity sang DTO kết quả
                resultList[i].IssuedAt = entities[i].IssuedAt;

                // Tiện tay bạn có thể gán thêm các trường mà Mapper cũ đang làm thiếu
                // Ví dụ: resultList[i].WarehouseName = entities[i].Warehouse?.Name;
            }

            return resultList;
        }
        #endregion

        #region Approve / Reject

        public async Task<GoodsIssueDto> CreateProductionGIAsync(
    ProductionGoodsIssueCreateDto dto)
        {
            var warehouse = await _dbContext.Warehouses
                .FirstOrDefaultAsync(w => w.Id == dto.WarehouseId);

            if (warehouse == null)
                throw new Exception("Kho không tồn tại");

            if (warehouse.WarehouseType != WarehouseType.RawMaterial)
                throw new Exception("Xuất sản xuất chỉ dùng kho nguyên liệu");

            var gi = new GoodsIssue
            {
                Id = Guid.NewGuid(),
                Code = GenerateGICode(),
                Type = GIType.Production,
                WarehouseId = dto.WarehouseId,
                Status = GIStatus.Pending,
                CreateAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Issued_Qty = 0,
                    Status = GIStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                }).ToList()
            };

            _dbContext.GoodsIssues.Add(gi);
            await _dbContext.SaveChangesAsync();

            // ✅ MAP SANG DTO
            return _mapper.Map<GoodsIssueDto>(gi);
        }


        public async Task<GoodsIssueDto> ApproveGIAsync(Guid giId)
        {
            // =====================================================
            // 1️⃣ Atomic approve (chống double click / double request)
            // =====================================================
            var affected = await _dbContext.GoodsIssues
                .Where(x => x.Id == giId && x.Status == GIStatus.Pending)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Status, GIStatus.Approve)
                    .SetProperty(x => x.UpdateAt, DateTime.UtcNow)
                );

            if (affected == 0)
                throw new Exception("GoodsIssue đã được approve hoặc không tồn tại");

            // =====================================================
            // 2️⃣ Load lại GI ở trạng thái KHÔNG TRACK
            //    => EF KHÔNG generate UPDATE ngoài ý muốn
            // =====================================================
            var gi = await _dbContext.GoodsIssues
                .AsNoTracking()
                .Include(x => x.Items)
                .Include(x => x.Warehouse)
                .FirstAsync(x => x.Id == giId);

            // =====================================================
            // 3️⃣ Validate nghiệp vụ
            // =====================================================
            if (gi.Type == GIType.Production &&
                gi.Warehouse.WarehouseType != WarehouseType.RawMaterial)
            {
                throw new Exception("Chỉ được xuất sản xuất từ kho nguyên liệu");
            }

            // =====================================================
            // 4️⃣ Allocate (IDEMPOTENT – gọi 2 lần cũng không sao)
            // =====================================================
            foreach (var item in gi.Items)
            {
                // Nếu đã có allocation thì skip
                var existed = await _dbContext.goodsIssueAllocates
                    .AnyAsync(a => a.GoodsIssueItemId == item.Id);

                if (existed)
                    continue;

                decimal remainingQty = item.Quantity;

                var locations = await _inventoryService.GetAvailableLocations(
                    item.ProductId,
                    gi.WarehouseId
                );

                foreach (var loc in locations)
                {
                    if (remainingQty <= 0)
                        break;

                    var allocQty = Math.Min(remainingQty, loc.AvailableQty);

                    _dbContext.goodsIssueAllocates.Add(new GoodsIssueAllocate
                    {
                        Id = Guid.NewGuid(),
                        GoodsIssueItemId = item.Id,
                        LocationId = loc.Id,
                        AllocatedQty = allocQty,
                        PickedQty = 0,
                        Status = GIAStatus.Planned
                    });

                    remainingQty -= allocQty;
                }

                // Không đủ tồn → allocate thiếu
                if (remainingQty > 0)
                {
                    _dbContext.goodsIssueAllocates.Add(new GoodsIssueAllocate
                    {
                        Id = Guid.NewGuid(),
                        GoodsIssueItemId = item.Id,
                        LocationId = null,
                        AllocatedQty = remainingQty,
                        PickedQty = 0,
                        Status = GIAStatus.Planned
                    });
                }
            }

            // =====================================================
            // 5️⃣ SaveChanges – CHỈ INSERT => KHÔNG CONCURRENCY
            // =====================================================
            await _dbContext.SaveChangesAsync();

            return MapToDto(gi);
        }
        private static GoodsIssueDto MapToDto(GoodsIssue gi)
        {
            return new GoodsIssueDto
            {
                Id = gi.Id,
                Code = gi.Code,
                SalesOrderId = gi.SalesOrderId,
                Type = gi.Type,
                WarehouseId = gi.WarehouseId,
                Status = gi.Status,
                CreatedAt = gi.CreateAt,
                UpdatedAt = gi.UpdateAt,
                IssuedAt = gi.IssuedAt,

                Items = gi.Items.Select(item => new GoodsIssueItemDto
                {
                    Id = item.Id,
                    GoodsIssueId = item.GoodsIssueId,
                    ProductId = item.ProductId,
                    SalesOrderItemId = item.SOIId,
                    LocationId = item.LocationId,
                    Quantity = item.Quantity,
                    IssuedQty = item.Issued_Qty,
                    Status = item.Status,
                    CreatedAt = item.CreatedAt,
                    UpdatedAt = item.UpdatedAt,

                    Allocations = item.Allocations.Select(a => new GoodsIssueAllocateDto
                    {
                        Id = a.Id,
                        GoodsIssueItemId = a.GoodsIssueItemId,
                        LocationId = a.LocationId ?? Guid.Empty,
                        AllocatedQty = a.AllocatedQty,
                        PickedQty = a.PickedQty,
                        Status = a.Status
                    }).ToList()

                }).ToList()
            };
        }

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
                    Type = GIType.Sale,
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
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    var gii = await _dbContext.GoodsIssueItems
                        .Include(x => x.Allocations)
                        .FirstOrDefaultAsync(s => s.Id == dto.Id);

                    var gi = await _dbContext.GoodsIssues.Include(s=> s.Warehouse).FirstOrDefaultAsync(s => s.Id == dto.GoodsIssueId);
                    var issued1Location = new Location();
                    try
                    {
                        var issuedLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                        issued1Location = issuedLocation;
                    }catch (Exception e)
                    {
                        throw new BusinessException(
                            "WAREHOUSE_SHIPPING_LOCATION_NOT_CONFIGURED",
                            $"Kho '{gi.Warehouse.Name}' chưa cấu hình vị trí xuất hàng (Shipping Location). " +
                            "Vui lòng kiểm tra và thiết lập Location loại Shipping/Output."
                        );

                    }


                    // Chuẩn bị danh sách ID để load một lần
                    var allocateIds = dto.Allocations.Select(x => x.Id).ToList();
                    var allAllocates = await _dbContext.goodsIssueAllocates
                        .Where(a => allocateIds.Contains(a.Id))
                        .ToListAsync();

                    foreach (var itemDto in dto.Allocations)
                    {
                        var gia = allAllocates.FirstOrDefault(a => a.Id == itemDto.Id);
                        if (gia == null) continue;

                        // Cập nhật PickedQty cho bản ghi hiện tại
                        decimal actualPicked = itemDto.PickedQty;
                        gia.PickedQty = actualPicked;
                        gia.Status = GIAStatus.Picked;

                        // 2. TRỪ kho tại Kệ và CỘNG vào Cổng xuất
                        await _inventoryService.AdjustPickingAsync(
                            gi.WarehouseId,      // 1
                            gia.LocationId,      // 2
                            dto.ProductId,       // 3
                            actualPicked,        // 4
                            InventoryActionType.AdjustDecrease, // 5
                            gi.Code,             // 6: refCode (Phải là string)
                            gia.LotId            // 7: lotId (Phải là Guid)
                        ); await _inventoryService.AdjustAsync(gi.WarehouseId, issued1Location.Id, dto.ProductId, actualPicked, InventoryActionType.AdjustIncrease);
                        // 3. Xử lý thiếu hàng (Re-allocate)
                        if (actualPicked < gia.AllocatedQty)
                        {
                            decimal remainingQty = gia.AllocatedQty - actualPicked;

                            // Tìm các vị trí còn hàng khác (Chỉ lấy các vị trí chưa được dùng trong đợt pick này để tránh duplicate)
                            var availableLocs = await _inventoryService.GetAvailableLocations(dto.ProductId, gi.WarehouseId);

                            foreach (var loc in availableLocs.Where(l => l.Id != gia.LocationId && l.Type == Enums.location.LocationType.Storage))
                            {
                                if (remainingQty <= 0) break;
                                decimal allocQty = Math.Min(remainingQty, loc.AvailableQty);
                                _dbContext.goodsIssueAllocates.Add(new GoodsIssueAllocate
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsIssueItemId = gii.Id,
                                    LocationId = loc.Id,
                                    AllocatedQty = allocQty,
                                    Status = GIAStatus.Planned
                                });
                                remainingQty -= allocQty;
                            }

                            // Nếu vẫn thiếu hàng ngay cả khi đã quét hết kho
                            if (remainingQty > 0)
                            {
                                _dbContext.goodsIssueAllocates.Add(new GoodsIssueAllocate
                                {
                                    Id = Guid.NewGuid(),
                                    GoodsIssueItemId = gii.Id,
                                    LocationId = null, // Đánh dấu chờ xử lý thủ công
                                    AllocatedQty = remainingQty,
                                    Status = GIAStatus.Planned
                                });
                            }
                        }
                    }

                    // Lưu tất cả thay đổi trong một lượt
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });
            
        }
        public async Task OutgoingStockCount(IssueGoodsDto dto)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                using var tx = await _dbContext.Database.BeginTransactionAsync();
                try
                {
                    // 1. Lấy dữ liệu Item và kiểm tra cơ bản
                    var gii = await _dbContext.GoodsIssueItems
                        .FirstOrDefaultAsync(x => x.Id == dto.GoodsIssueItemId);

                    if (gii == null) throw new Exception("Không tìm thấy dòng hàng xuất kho.");
                    if (dto.IssuedQty <= 0) throw new Exception("Số lượng xuất phải lớn hơn 0.");

                    // Kiểm tra tổng số lượng đã xuất so với yêu cầu
                    if (gii.Issued_Qty + dto.IssuedQty > gii.Quantity)
                        throw new Exception("Tổng số lượng xuất vượt quá số lượng yêu cầu trên phiếu.");

                    // 2. Lấy danh sách các lô hàng đã được Pick (đang nằm ở cổng xuất)
                    var pickedAllocates = await _dbContext.goodsIssueAllocates
                        .Where(x => x.GoodsIssueItemId == gii.Id && x.Status == GIAStatus.Picked)
                        .OrderBy(x => x.LotId) // Sắp xếp để trừ có thứ tự
                        .ToListAsync();

                    // Tính số lượng thực tế đang nằm chờ ở cổng xuất (Picked - Đã xuất trước đó)
                    decimal totalCurrentlyAtGate = pickedAllocates.Sum(x => x.PickedQty) - gii.Issued_Qty;

                    if (dto.IssuedQty > totalCurrentlyAtGate)
                        throw new Exception("Số lượng xuất vượt quá số lượng hàng đang có sẵn tại cổng xuất (Số lượng đã Picking).");

                    // 3. Lấy thông tin phiếu xuất và vị trí xuất
                    var gi = await _dbContext.GoodsIssues
                        .FirstOrDefaultAsync(x => x.Id == gii.GoodsIssueId);

                    var issueLocation = await _warehouse.GetIssuedLocationId(gi.WarehouseId);
                    if (issueLocation == null)
                        throw new Exception("Kho chưa cấu hình vị trí xuất hàng (Issue Location).");

                    // 4. TRỪ TỒN KHO TẠI CỔNG XUẤT THEO TỪNG LÔ (LOT)
                    decimal qtyRemainingToIssue = dto.IssuedQty;

                    foreach (var alloc in pickedAllocates)
                    {
                        if (qtyRemainingToIssue <= 0) break;

                        // Xác định số lượng còn lại trong lô này tại cổng xuất có thể trừ
                        // (Lưu ý: Nếu một lô được pick nhiều lần, cần logic trừ chính xác hơn, 
                        // nhưng ở đây giả định trừ dần theo danh sách pickedAllocates)
                        decimal availableInThisAlloc = alloc.PickedQty;

                        decimal takeFromThisLot = Math.Min(qtyRemainingToIssue, availableInThisAlloc);
                        if (takeFromThisLot <= 0) continue;

                        await _inventoryService.AdjustAsync(
                            gi.WarehouseId,
                            issueLocation.Id,
                            gii.ProductId,
                            takeFromThisLot,
                            InventoryActionType.Issue,
                            refCode: gi.Code,
                            lotId: alloc.LotId // <--- TRỪ ĐÚNG LOTID ĐÃ PICK
                        );

                        qtyRemainingToIssue -= takeFromThisLot;
                    }

                    // 5. Cập nhật số lượng và trạng thái của GoodsIssueItem
                    gii.Issued_Qty += dto.IssuedQty;
                    gii.Status = gii.Issued_Qty >= gii.Quantity
                        ? GIStatus.Complete
                        : GIStatus.Partically_Issued;

                    // 6. Cập nhật trạng thái của GoodsIssue (Phiếu xuất)
                    var isGiComplete = !await _dbContext.GoodsIssueItems
                        .AnyAsync(x => x.GoodsIssueId == gi.Id && x.Status != GIStatus.Complete);

                    gi.Status = isGiComplete ? GIStatus.Complete : GIStatus.Partically_Issued;
                    gi.IssuedAt = DateTime.UtcNow;

                    // 7. Cập nhật SalesOrderItem (Nếu là đơn bán hàng)
                    if (gi.Type == GIType.Sale)
                    {
                        var soi = await _dbContext.SalesOrderItems
                            .FirstOrDefaultAsync(s => s.Id == gii.SOIId);

                        if (soi != null)
                        {
                            soi.Issued_Qty += dto.IssuedQty;
                            soi.Status = soi.Issued_Qty >= soi.Quantity
                                ? SOStatus.Complete
                                : SOStatus.Partically_Issued;

                            var so = await _dbContext.SalesOrders.FirstOrDefaultAsync(s => s.Id == soi.SalesOrderId);
                            if (so != null)
                            {
                                var isSoComplete = !await _dbContext.SalesOrderItems
                                    .AnyAsync(s => s.SalesOrderId == so.Id && s.Status != SOStatus.Complete);

                                so.Status = isSoComplete ? SOStatus.Complete : SOStatus.Partically_Issued;
                            }
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception)
                {
                    await tx.RollbackAsync();
                    throw;
                }
            });
        }
        public async Task<GoodsIssue> CreateGIAsync(GoodsIssueDto dto)
        {
            var warehousecheck = await _dbContext.Warehouses.FirstOrDefaultAsync(s => s.Id == dto.WarehouseId);
            if (warehousecheck.WarehouseType != WarehouseType.RawMaterial)
                throw new Exception("Không thể xuất kho không thuộc loại vật liệu");
            var GI = new GoodsIssue
            {
                Id = dto.Id,
                Code = GenerateGICode(),
                SalesOrderId = dto.SalesOrderId,
                Type = GIType.Production,
                Status = dto.Status,
                CreateAt = DateTime.UtcNow,
                Items = dto.Items.Select(i => new GoodsIssueItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Issued_Qty = 0,
                    GoodsIssueId= dto.Id,
                    Status = GIStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                }).ToList()
            };
            _dbContext.GoodsIssues.Add(GI);
            await _dbContext.SaveChangesAsync();

            return GI;
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
