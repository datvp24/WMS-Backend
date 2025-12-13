namespace Wms.Application.DTOs.MasterData.Brands;

public class UpdateBrandDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
