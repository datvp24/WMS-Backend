using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Wms.Application.DTOS.Sales;

// DTO dùng để trả về thông tin SO chi tiết
public class SalesOrderDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public bool LockedStock { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public List<SalesOrderItemDto> Items { get; set; } = new();
    public List<GoodsIssueDto> GoodsIssues { get; set; } = new();
}

// DTO trả về chi tiết item của SO
public class SalesOrderItemDto
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}

// DTO dùng để tạo SO
public class SalesOrderCreateDto
{
    [Required]
    public int CustomerId { get; set; }

    [Required]
    public List<SalesOrderItemCreateDto> Items { get; set; } = new();
}

public class SalesOrderItemCreateDto
{
    [Required]
    public int ProductId { get; set; }
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

// DTO dùng để cập nhật SO (có thể chỉ cho phép thay đổi qty/unitprice trước khi lock stock)
public class SalesOrderUpdateDto
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public List<SalesOrderItemUpdateDto> Items { get; set; } = new();
}

public class SalesOrderItemUpdateDto
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}

// DTO dùng cho query/filter SO
public class SalesOrderQueryDto
{
    public string? Code { get; set; }
    public int? CustomerId { get; set; }
    public string? Status { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
