using Wms.Application.DTOs.System.Roles;

namespace Wms.Application.Interfaces.Services.System;

public interface IRoleService
{
    Task<int> CreateRoleAsync(CreateRoleDto dto);
    Task UpdateRoleAsync(int id, UpdateRoleDto dto);
    Task DeleteRoleAsync(int id);

    Task AssignPermissionAsync(AssignPermissionToRoleDto dto);
    Task RemovePermissionAsync(int roleId, int permissionId);

    Task<RoleDetailDto> GetRoleAsync(int id);
    Task<List<RoleDetailDto>> GetAllAsync();
}
