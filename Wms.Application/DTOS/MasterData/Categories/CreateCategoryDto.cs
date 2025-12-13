namespace Wms.Application.DTOs.MasterData.Categories;

public class CreateCategoryDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
