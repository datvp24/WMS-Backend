using AutoMapper;
using Wms.Application.DTOS.Sales;
using Wms.Domain.Entity.MasterData;
using Wms.Domain.Entity.Sales;
using Wms.Domain.Entity.Warehouses;

namespace Wms.Application.Mapper.Sales
{
    public class SalesMappingProfile : Profile
    {
        public SalesMappingProfile()
        {
            // SalesOrder ↔ SalesOrderDto
            CreateMap<SalesOrder, SalesOrderDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.GoodsIssues, opt => opt.MapFrom(src => src.GoodsIssues));

            // SalesOrderItem → SalesOrderItemDto
            CreateMap<SalesOrderItem, SalesOrderItemDto>()
                .ForMember(dest => dest.OrderQty, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.IssuedQty, opt => opt.MapFrom(src => src.Issued_Qty));

            // Create/Update DTO → Entity
            CreateMap<SalesOrderCreateDto, SalesOrder>();
            CreateMap<SalesOrderItemCreateDto, SalesOrderItem>();
            CreateMap<SalesOrderUpdateDto, SalesOrder>();
            CreateMap<SalesOrderItemUpdateDto, SalesOrderItem>();

            // GoodsIssue ↔ GoodsIssueDto
            CreateMap<GoodsIssue, GoodsIssueDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // Create DTO → Entity
            CreateMap<GoodsIssueItem, GoodsIssueItemDto>(); // 🔥 DÒNG BỊ THIẾU

            CreateMap<GoodsIssueCreateDto, GoodsIssue>();
            CreateMap<GoodsIssueItemCreateDto, GoodsIssueItem>();
            CreateMap<GoodsIssueAllocate, GoodsIssueAllocate1Dto>()
    .ForMember(d => d.LocationId, o => o.MapFrom(s => s.LocationId)) // Lấy trực tiếp FK
    .ForMember(d => d.LocationCode, o => o.MapFrom(s => s.Location.Code)); // Lấy qua Navigation
        }
    }

}
