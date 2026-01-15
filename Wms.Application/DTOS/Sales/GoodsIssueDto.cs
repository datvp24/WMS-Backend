using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Wms.Domain.Entity.Sales;

namespace Wms.Application.DTOS.Sales
{
    // DTO trả về chi tiết GI
    public class GoodsIssueDto
    {
        public Guid Id { get; set; }
        public string? Code { get; set; } = null!;
        public Guid SalesOrderId { get; set; }
        public Guid WarehouseId { get; set; }
        public GIStatus Status { get; set; }
        public DateTime CreateAt { get; set; }
        public DateTime UpdateAt { get; set; }
        public DateTime IssuedAt { get; set; }
        public List<GoodsIssueItemDto> Items { get; set; } = new();
    }

    // DTO chi tiết item GI
    public class GoodsIssueItemDto
    {
        public Guid Id { get; set; }
        public int ProductId { get; set; }
        public Guid GoodsIssueId { get; set; }
        public Guid SOId { get; set; }
        public int Quantity { get; set; }
        public int Issued_Qty { get; set; }
        public GIStatus Status { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public List<GoodsIssueAllocateDto> Items { get; set; }
    }
    public class IssueGoodsDto
    {
        public Guid GoodsIssueItemId { get; set; }
        public int IssuedQty { get; set; }
    }

    public class GoodsIssueAllocateDto
    {
        public Guid Id { get; set; }
        public Guid GoodsIssueItemId { get; set; }
        public Guid LocationId { get; set; }  // vị trí trong kho
        public decimal AllocatedQty { get; set; }  // số lượng phân bổ tại location

        public decimal PickedQty { get; set; }
        public GIAStatus Status { get; set; } = GIAStatus.Planned;

    }

    // DTO dùng để tạo GI từ SO đã approve
    public class GoodsIssueCreateDto
    {
        [Required]
        public Guid SalesOrderId { get; set; }
        [Required]
        public Guid WarehouseId { get; set; }
        [Required]
        public List<GoodsIssueItemCreateDto> Items { get; set; } = new();
    }

    public class GoodsIssueItemCreateDto
    {
        [Required]
        public int ProductId { get; set; }
        [Required]
        public Guid LocationId { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    // DTO query/filter GI
    public class GoodsIssueQueryDto
    {
        public string? Code { get; set; }
        public Guid? SalesOrderId { get; set; }
        public GIStatus? Status { get; set; }
        public DateTime? IssuedFrom { get; set; }
        public DateTime? IssuedTo { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
