namespace Wms.Application.DTOs.MasterData.Units;

public class UnitDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public DateTime CreateAt { get; set; }
}
