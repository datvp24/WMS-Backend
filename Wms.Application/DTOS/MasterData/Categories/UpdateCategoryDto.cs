namespace Wms.Application.DTOs.MasterData.Categories;

public class UpdateCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; }
    public bool IsActive { get; set; }
}
