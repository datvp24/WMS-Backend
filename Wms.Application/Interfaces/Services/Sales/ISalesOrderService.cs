using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Domain.Entity.Sales;

namespace Wms.Application.Interfaces.Service.Sales
{
    public interface ISalesOrderService
    {
        Task<SalesOrderDto> CreateSOAsync(SalesOrderDto dto);
        Task<SalesOrderDto> ApproveSOAsync(Guid soId);
        Task<SalesOrderDto> RejectSOAsync(Guid soId);
        Task<SalesOrderDto> GetSOAsync(Guid soId);
        Task OutgoingStockCount(IssueGoodsDto dto);
        Task<List<GoodsIssueDto>> QueryGoodsIssuesAsync(GoodsIssueQuery1Dto dto);
        Task<List<SalesOrderDto>> QuerySOsAsync(SalesOrderQueryDto dto);
        Task Picking(GoodsIssueItemDto dto);
        Task<GoodsIssueDetailDto?> GetGoodsIssueDetailAsync(Guid goodsIssueId);
        Task<GoodsIssueDto> CreateProductionGIAsync(
            ProductionGoodsIssueCreateDto dto);
        Task<GoodsIssueDto> ApproveGIAsync(Guid giId);
    }
}
