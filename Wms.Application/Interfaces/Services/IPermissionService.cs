using Wms.Application.DTOs.System.Permissions;

namespace Wms.Application.Interfaces.Services.System;

public interface IPermissionService
{
    Task<int> CreateAsync(CreatePermissionDto dto);
    Task UpdateAsync(int id, UpdatePermissionDto dto);
    Task DeleteAsync(int id);
    Task<List<PermissionDto>> GetAllAsync();
}
