using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Service.Sales;
using Wms.Infrastructure.Persistence.Context;
using Wms.Domain.Entity.Sales;
using Wms.Application.Interfaces.Services.Inventory;

namespace Wms.Domain.Service.Sales
{
    public class SalesOrderService : ISalesOrderService
    {
        private readonly AppDbContext _dbContext;
        private readonly IInventoryService inventoryService;
        private readonly IMapper _mapper;

        public SalesOrderService(AppDbContext dbContext, IMapper mapper, IInventoryService _inventory)
        {
            inventoryService = _inventory;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        #region Create / Update / Get

        public async Task<SalesOrderDto> CreateSOAsync(SalesOrderCreateDto dto)
        {
            var entity = _mapper.Map<SalesOrder>(dto);

            // 1. Tự động sinh mã Code nếu Frontend không gửi lên
            if (string.IsNullOrEmpty(entity.Code))
            {
                string datePart = DateTime.Now.ToString("yyyyMMdd");
                // Đếm số đơn trong ngày để lấy số thứ tự
                int countToday = await _dbContext.SalesOrders
                    .CountAsync(x => x.CreatedAt.Date == DateTime.Today);
                entity.Code = $"SO-{datePart}-{(countToday + 1).ToString("D4")}";
            }

            entity.Status = "DRAFT";

            // 2. Tính toán TotalPrice cho từng dòng Item (Rất quan trọng)
            foreach (var item in entity.Items)
            {
                var inventory = await _dbContext.Inventories
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ProductId == item.ProductId);

                if (inventory == null)
                    throw new Exception($"Không tìm thấy tồn kho cho sản phẩm {item.ProductId}");

                var availableQty = inventory.OnHandQuantity - inventory.LockedQuantity;

                if (item.Quantity > availableQty)
                    throw new Exception(
                        $"Sản phẩm {item.ProductId} chỉ còn {availableQty}"
                    );

                item.TotalPrice = item.Quantity * item.UnitPrice;
            }


            entity.TotalAmount = entity.Items.Sum(i => i.TotalPrice);

            _dbContext.SalesOrders.Add(entity);
            await _dbContext.SaveChangesAsync();

            // 3. Load thêm thông tin khách hàng để trả về DTO không bị null CustomerName
            await _dbContext.Entry(entity).Reference(x => x.Customer).LoadAsync();

            return _mapper.Map<SalesOrderDto>(entity);
        }

        public async Task<SalesOrderDto> UpdateSOAsync(SalesOrderUpdateDto dto)
        {
            var entity = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (entity == null)
                throw new Exception("SalesOrder not found");

            if (entity.Status != "DRAFT")
                throw new Exception("Only DRAFT orders can be updated");

            foreach (var itemDto in dto.Items)
            {
                var item = entity.Items.FirstOrDefault(x => x.Id == itemDto.Id);
                if (item != null)
                {
                    item.Quantity = itemDto.Quantity;
                    item.UnitPrice = itemDto.UnitPrice;
                    item.TotalPrice = itemDto.Quantity * itemDto.UnitPrice;
                }
            }

            entity.TotalAmount = entity.Items.Sum(x => x.TotalPrice);
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SalesOrderDto>(entity);
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

            if (!string.IsNullOrEmpty(dto.Status))
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

        #endregion

        #region Approve / Reject

        public async Task<SalesOrderDto> ApproveSOAsync(Guid soId, Guid managerId)
        {
            var entity = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == soId);

            if (entity == null)
                throw new Exception("SalesOrder not found");

            if (entity.Status != "DRAFT")
                throw new Exception("Only DRAFT orders can be approved");

            entity.Status = "APPROVED";
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return _mapper.Map<SalesOrderDto>(entity);
        }

        public async Task<SalesOrderDto> RejectSOAsync(Guid soId)
        {
            var entity = await _dbContext.SalesOrders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x => x.Id == soId);

            if (entity == null)
                throw new Exception("SalesOrder not found");

            if (entity.Status != "APPROVED" && entity.Status != "DRAFT")
                throw new Exception("Only APPROVED or DRAFT orders can be rejected");

            entity.Status = "REJECTED";
            entity.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return _mapper.Map<SalesOrderDto>(entity);
        }

        #endregion
    }
}
