using System.Collections.Generic;
using Wms.Application.DTOs.System.Permissions;

namespace Wms.Application.DTOs.System.Roles;

public class RoleDetailDto
{
    public int Id { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();

    // Thêm các trường audit
    public DateTime CreatedAt { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }
}
