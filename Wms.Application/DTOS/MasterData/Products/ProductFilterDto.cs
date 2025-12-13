namespace Wms.Application.DTOs.MasterData.Products;

public class ProductFilterDto
{
    public string? Keyword { get; set; }
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }
    public int? SupplierId { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
