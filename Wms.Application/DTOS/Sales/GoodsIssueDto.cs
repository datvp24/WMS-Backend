using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Wms.Application.DTOS.Sales
{
    // DTO trả về chi tiết GI
    public class GoodsIssueDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public Guid SalesOrderId { get; set; }
        public string SalesOrderCode { get; set; } = null!;
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime IssuedAt { get; set; }

        public List<GoodsIssueItemDto> Items { get; set; } = new();
    }

    // DTO chi tiết item GI
    public class GoodsIssueItemDto
    {
        public Guid Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public Guid LocationId { get; set; }
        public string LocationCode { get; set; } = null!;
        public int Quantity { get; set; }
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
        public string? Status { get; set; }
        public DateTime? IssuedFrom { get; set; }
        public DateTime? IssuedTo { get; set; }

        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
