using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wms.Application.DTOS.Sales;
using Wms.Domain.Entity.Sales;

namespace Wms.Application.Mapper.Sales
{
    public class GoodsIssueProfile : Profile
    {
        public GoodsIssueProfile()
        {
            CreateMap<GoodsIssue, GoodsIssueDto>();

            CreateMap<GoodsIssueItem, GoodsIssueItemDto>()
                .ForMember(d => d.SalesOrderItemId,
                    o => o.MapFrom(s => s.SOIId))
                .ForMember(d => d.IssuedQty,
                    o => o.MapFrom(s => s.Issued_Qty))
                .ForMember(d => d.Allocations,
                    o => o.MapFrom(s => s.Allocations));

            CreateMap<GoodsIssueAllocate, GoodsIssueAllocateDto>();
        }
    }
}
