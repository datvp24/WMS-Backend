namespace Wms.Application.DTOs.MasterData.Suppliers;

public class CreateSupplierDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
}
