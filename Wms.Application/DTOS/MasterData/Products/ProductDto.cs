using System.Runtime.CompilerServices;
using Wms.Domain.Entity.MasterData;

namespace Wms.Application.DTOs.MasterData.Products;

public class ProductDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public ProductType Type { get; set; }
    public int CategoryId { get; set; }
    public int UnitId { get; set; }
    public int BrandId { get; set; }
    public int SupplierId { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreateAt { get; set; }
}
public class ProductSupDto
{
    public int SupplierId { get; set; }
}
public class ProductTypeDto
{
    public ProductType Type { get; set; } 
}
