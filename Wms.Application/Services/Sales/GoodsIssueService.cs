using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Application.Interfaces.Services.Inventory;
using Wms.Application.Interfaces.Services.Sales;
using Wms.Domain.Entity.Sales;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Sales
{
    public class GoodsIssueService : IGoodsIssueService
    {
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IInventoryService _inventoryService;

        public GoodsIssueService(AppDbContext dbContext, IMapper mapper, IInventoryService inventoryService)
        {
            _dbContext = dbContext;
            _mapper = mapper;
            _inventoryService = inventoryService;
        }
        public async Task<GoodsIssueDto> CancelGIAsync(Guid giId)
        {
            var gi = await _dbContext.GoodsIssues
                .Include(x => x.Items)
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == giId);

            if (gi == null) throw new Exception("GoodsIssue not found");

            // Chỉ cancel GI chưa completed
            if (gi.Status == "COMPLETED")
                throw new Exception("Completed GoodsIssue cannot be cancelled");

            if (gi.Status == "CANCELLED")
                throw new Exception("GoodsIssue is already cancelled");

            // Nếu muốn unlock stock (nếu đã lock)
            foreach (var item in gi.Items)
            {
                await _inventoryService.UnlockStockAsync(
                    gi.WarehouseId,
                    item.LocationId,
                    item.ProductId,
                    item.Quantity
                );
            }

            gi.Status = "CANCELLED";
            await _dbContext.SaveChangesAsync();

            return _mapper.Map<GoodsIssueDto>(gi);
        }



        #region Create / Get / Query

        public async Task<GoodsIssueDto> CreateGIAsync(GoodsIssueCreateDto dto)
        {
            // Bắt đầu Transaction để đảm bảo: Tạo GI thành công THÌ phải Khóa kho thành công
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var so = await _dbContext.SalesOrders
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == dto.SalesOrderId);

                if (so == null) throw new Exception("SalesOrder not found");
                if (so.Status != "APPROVED") throw new Exception("Only APPROVED SalesOrder can create GI");

                var code = $"GI-{DateTime.UtcNow:yyyyMMddHHmmss}";

                var gi = new GoodsIssue
                {
                    SalesOrderId = so.Id,
                    Code = code,
                    WarehouseId = dto.WarehouseId,
                    Status = "PENDING",
                    IssuedAt = DateTime.UtcNow,
                    Items = dto.Items.Select(i => new GoodsIssueItem
                    {
                        ProductId = i.ProductId,
                        LocationId = i.LocationId,
                        Quantity = i.Quantity
                    }).ToList()
                };

                _dbContext.GoodsIssues.Add(gi);
                await _dbContext.SaveChangesAsync();

                // --- LOGIC KHÓA KHO ---
                foreach (var item in gi.Items)
                {
                    await _inventoryService.LockStockAsync(
                        gi.WarehouseId,
                        item.LocationId,
                        item.ProductId,
                        item.Quantity,
                        $"Locked for Goods Issue: {gi.Code}"
                    );
                }

                await transaction.CommitAsync();
                return _mapper.Map<GoodsIssueDto>(gi);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<GoodsIssueDto> GetGIAsync(Guid giId)
        {
            var gi = await _dbContext.GoodsIssues
                .Include(x => x.Items)
                .Include(x => x.SalesOrder)
                .Include(x => x.Warehouse)
                .FirstOrDefaultAsync(x => x.Id == giId);

            if (gi == null) throw new Exception("GoodsIssue not found");

            return _mapper.Map<GoodsIssueDto>(gi);
        }

        public async Task<List<GoodsIssueDto>> QueryGIsAsync(GoodsIssueQueryDto dto)
        {
            var query = _dbContext.GoodsIssues
                .Include(x => x.Items)
                .Include(x => x.SalesOrder)
                .Include(x => x.Warehouse)
                .AsQueryable();

            if (!string.IsNullOrEmpty(dto.Code)) query = query.Where(x => x.Code.Contains(dto.Code));
            if (dto.SalesOrderId.HasValue) query = query.Where(x => x.SalesOrderId == dto.SalesOrderId.Value);
            if (!string.IsNullOrEmpty(dto.Status)) query = query.Where(x => x.Status == dto.Status);
            if (dto.IssuedFrom.HasValue) query = query.Where(x => x.IssuedAt >= dto.IssuedFrom.Value);
            if (dto.IssuedTo.HasValue) query = query.Where(x => x.IssuedAt <= dto.IssuedTo.Value);

            var list = await query
                .OrderByDescending(x => x.IssuedAt)
                .Skip((dto.PageIndex - 1) * dto.PageSize)
                .Take(dto.PageSize)
                .ToListAsync();

            return _mapper.Map<List<GoodsIssueDto>>(list);
        }

        #endregion

        #region Complete GI / Inventory Adjustment

        public async Task<GoodsIssueDto> CompleteGIAsync(Guid giId)
        {
            var gi = await _dbContext.GoodsIssues
                .Include(x => x.Items)
                .Include(x => x.SalesOrder)
                .FirstOrDefaultAsync(x => x.Id == giId);

            if (gi == null) throw new Exception("GoodsIssue not found");
            if (gi.Status != "PENDING") throw new Exception("Only PENDING GoodsIssue can be completed");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in gi.Items)
                {
                    // 1. MỞ KHÓA (Giảm LockedQuantity)
                    await _inventoryService.UnlockStockAsync(
                        gi.WarehouseId,
                        item.LocationId,
                        item.ProductId,
                        item.Quantity,
                        $"Unlock for completion of: {gi.Code}"
                    );

                    // 2. TRỪ KHO THỰC TẾ (Giảm OnHandQuantity)
                    await _inventoryService.AdjustAsync(
                        gi.WarehouseId,
                        item.LocationId,
                        item.ProductId,
                        -item.Quantity,
                        Wms.Domain.Enums.Inventory.InventoryActionType.Issue,
                        gi.Code
                    );
                }

                gi.Status = "COMPLETED";
                gi.IssuedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return _mapper.Map<GoodsIssueDto>(gi);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        #endregion
    }
}
