namespace Wms.Application.DTOs.MasterData.Brands;

public class BrandDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}
