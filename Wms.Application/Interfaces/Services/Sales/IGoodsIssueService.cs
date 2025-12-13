using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;

namespace Wms.Application.Interfaces.Services.Sales
{
    public interface IGoodsIssueService
    {
        Task<GoodsIssueDto> CreateGIAsync(GoodsIssueCreateDto dto);
        Task<GoodsIssueDto> CompleteGIAsync(Guid giId);
        Task<GoodsIssueDto> GetGIAsync(Guid giId);
        Task<List<GoodsIssueDto>> QueryGIsAsync(GoodsIssueQueryDto dto);
        Task<GoodsIssueDto> CancelGIAsync(Guid giId);

    }
}
