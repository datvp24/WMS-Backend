namespace Wms.Application.DTOs.MasterData.Categories;

public class CategoryDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set;}
    public DateTime CreateAt { get; set; }
}
