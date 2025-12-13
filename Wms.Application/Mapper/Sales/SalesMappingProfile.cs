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
                .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.Name))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
                .ForMember(dest => dest.GoodsIssues, opt => opt.MapFrom(src => src.GoodsIssues));

            CreateMap<SalesOrderItem, SalesOrderItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name));

            // Create/Update DTO → Entity
            CreateMap<SalesOrderCreateDto, SalesOrder>();
            CreateMap<SalesOrderItemCreateDto, SalesOrderItem>();
            CreateMap<SalesOrderUpdateDto, SalesOrder>();
            CreateMap<SalesOrderItemUpdateDto, SalesOrderItem>();

            // GoodsIssue ↔ GoodsIssueDto
            CreateMap<GoodsIssue, GoodsIssueDto>()
                .ForMember(dest => dest.SalesOrderCode, opt => opt.MapFrom(src => src.SalesOrder.Code))
                .ForMember(dest => dest.WarehouseName, opt => opt.MapFrom(src => src.Warehouse.Name))
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            CreateMap<GoodsIssueItem, GoodsIssueItemDto>()
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
                .ForMember(dest => dest.LocationCode, opt => opt.MapFrom(src => src.Location.Code));

            // Create DTO → Entity
            CreateMap<GoodsIssueCreateDto, GoodsIssue>();
            CreateMap<GoodsIssueItemCreateDto, GoodsIssueItem>();
        }
    }
}
