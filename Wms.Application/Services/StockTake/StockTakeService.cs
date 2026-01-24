//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Wms.Application.DTOS.StockTake;
//using Wms.Application.Interfaces.Services.Inventory;
//using Wms.Application.Interfaces.Services.StockTake;
//using Wms.Domain.Entity.StockTakes;
//using Wms.Domain.Enums.Inventory;
//using Microsoft.EntityFrameworkCore;
//using Wms.Domain.Enums.StockTakes;
//using Wms.Infrastructure.Persistence.Context;
//using Wms.Domain.Entity.StockTakes;

//namespace Wms.Application.Services.StockTake
//{
//    public class StockTakeService : IStockTakeService
//    {
//        private readonly AppDbContext _db;
//        private readonly IInventoryService _inventoryService;

//        public StockTakeService(AppDbContext db, IInventoryService inventoryService)
//        {
//            _db = db;
//            _inventoryService = inventoryService;
//        }

//        // 1. Tạo phiếu nháp
//        public async Task<StockTakeDto> CreateAsync(CreateStockTakeDto dto)
//        {
//            var stockTake = new Domain.Entity.StockTakes.StockTake
//            {
//                Code = $"ST-{DateTime.UtcNow:yyyyMMdd-HHmm}",
//                WarehouseId = dto.WarehouseId,
//                Description = dto.Description,
//                Status = StockTakeStatus.Draft,
//                CreatedAt = DateTime.UtcNow
//            };

//            _db.StockTakes.Add(stockTake);
//            await _db.SaveChangesAsync();
//            return await GetByIdAsync(stockTake.Id);
//        }

//        // 2. Chốt số liệu (Snapshot)
//        public async Task<StockTakeDto> StartAsync(Guid id)
//        {
//            var st = await _db.StockTakes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
//            if (st == null || st.Status != StockTakeStatus.Draft) throw new Exception("Phiếu không hợp lệ.");

//            // Lấy tất cả tồn kho của kho này
//            var currentStocks = await _db.Inventories
//                .Where(x => x.WarehouseId == st.WarehouseId && x.OnHandQuantity > 0)
//                .ToListAsync();

//            foreach (var s in currentStocks)
//            {
//                st.Items.Add(new StockTakeItem
//                {
//                    StockTakeId = st.Id,
//                    LocationId = s.LocationId,
//                    ProductId = s.ProductId,
//                    SystemQty = s.OnHandQuantity,
//                    CountedQty = s.OnHandQuantity // Mặc định bằng nhau để user sửa sau
//                });
//            }

//            st.Status = StockTakeStatus.InProgress;
//            await _db.SaveChangesAsync();
//            return await GetByIdAsync(id);
//        }

//        // 3. Cập nhật số lượng đếm được (Dùng cho nhân viên kho nhập liệu)
//        public async Task<StockTakeDto> UpdateCountsAsync(SubmitCountDto dto)
//        {
//            var items = await _db.StockTakeItems
//                .Where(x => x.StockTakeId == dto.StockTakeId)
//                .ToListAsync();

//            foreach (var count in dto.Counts)
//            {
//                var item = items.FirstOrDefault(x => x.ProductId == count.ProductId && x.LocationId == count.LocationId);
//                if (item != null)
//                {
//                    item.CountedQty = count.CountedQty;
//                    item.Note = count.Note;
//                }
//            }

//            await _db.SaveChangesAsync();
//            return await GetByIdAsync(dto.StockTakeId);
//        }

//        // 4. Hoàn tất và tự động điều chỉnh kho
//        public async Task<StockTakeDto> CompleteAsync(Guid id)
//        {
//            var st = await _db.StockTakes.Include(x => x.Items).FirstOrDefaultAsync(x => x.Id == id);
//            if (st == null || st.Status != StockTakeStatus.InProgress) throw new Exception("Trạng thái không hợp lệ.");

//            using var trans = await _db.Database.BeginTransactionAsync();
//            try
//            {
//                foreach (var item in st.Items)
//                {
//                    decimal diff = item.CountedQty - item.SystemQty;
//                    if (diff != 0)
//                    {
//                        await _inventoryService.AdjustAsync(st.WarehouseId, item.LocationId, item.ProductId, diff, InventoryActionType.StockTakeAdjustment, st.Code);
//                    }
//                }
//                st.Status = StockTakeStatus.Completed;
//                st.CompletedAt = DateTime.UtcNow;
//                await _db.SaveChangesAsync();
//                await trans.CommitAsync();
//                return await GetByIdAsync(id);
//            }
//            catch
//            {
//                await trans.RollbackAsync();
//                throw;
//            }
//        }

//        // 5. Get Detail & List (Mapping)
//        public async Task<StockTakeDto> GetByIdAsync(Guid id)
//        {
//            var t = await _db.StockTakes
//                .Include(x => x.Warehouse)
//                .Include(x => x.Items).ThenInclude(i => i.Product)
//                .Include(x => x.Items).ThenInclude(i => i.Location)
//                .FirstOrDefaultAsync(x => x.Id == id);

//            if (t == null) return null!;

//            return new StockTakeDto
//            {
//                Id = t.Id,
//                Code = t.Code,
//                WarehouseName = t.Warehouse?.Name,
//                Status = t.Status.ToString(),
//                CreatedAt = t.CreatedAt,
//                Items = t.Items.Select(i => new StockTakeItemDto
//                {
//                    LocationCode = i.Location?.Code,
//                    ProductName = i.Product?.Name,
//                    SystemQty = i.SystemQty,
//                    CountedQty = i.CountedQty,
//                    Difference = i.Difference,
//                    Note = i.Note
//                }).ToList()
//            };
//        }

//        public async Task<List<StockTakeDto>> GetListAsync(int page = 1, int pageSize = 20)
//        {
//            return await _db.StockTakes
//                .Include(x => x.Warehouse)
//                .OrderByDescending(x => x.CreatedAt)
//                .Skip((page - 1) * pageSize).Take(pageSize)
//                .Select(t => new StockTakeDto
//                {
//                    Id = t.Id,
//                    Code = t.Code,
//                    WarehouseName = t.Warehouse.Name,
//                    Status = t.Status.ToString(),
//                    CreatedAt = t.CreatedAt
//                }).ToListAsync();
//        }
//    }
//}
