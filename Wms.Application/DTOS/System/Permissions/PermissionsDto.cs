namespace Wms.Application.DTOs.System.Permissions;

public class PermissionDto
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }

    public bool IsDeleted { get; set; }
}
