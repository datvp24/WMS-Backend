using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;

namespace Wms.Application.Interfaces.Service.Sales
{
    public interface ISalesOrderService
    {
        Task<SalesOrderDto> CreateSOAsync(SalesOrderCreateDto dto);
        Task<SalesOrderDto> ApproveSOAsync(Guid soId, Guid managerId);
        Task<SalesOrderDto> RejectSOAsync(Guid soId);
        Task<SalesOrderDto> UpdateSOAsync(SalesOrderUpdateDto dto);
        Task<SalesOrderDto> GetSOAsync(Guid soId);
        Task<List<SalesOrderDto>> QuerySOsAsync(SalesOrderQueryDto dto);
    }
}
