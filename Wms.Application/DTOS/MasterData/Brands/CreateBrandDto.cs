namespace Wms.Application.DTOs.MasterData.Brands;

public class CreateBrandDto
{
    public string? Code { get; set; }      // optional
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
