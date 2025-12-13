using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.System.Roles;
using Wms.Application.Interfaces.Services.System;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _role;

    public RoleController(IRoleService role)
    {
        _role = role;
    }

    [HasPermission("role.create")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleDto dto)
        => Ok(await _role.CreateRoleAsync(dto));
    [HasPermission("role.update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateRoleDto dto)
    {
        await _role.UpdateRoleAsync(id, dto);
        return Ok();
    }
    [HasPermission("role.delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _role.DeleteRoleAsync(id);
        return Ok();
    }
    [HasPermission("role.assign-permission")]
    [HttpPost("assign-permission")]
    public async Task<IActionResult> AssignPermission(AssignPermissionToRoleDto dto)
    {
        await _role.AssignPermissionAsync(dto);
        return Ok();
    }
    [HasPermission("role.remove-permission")]
    [HttpDelete("remove-permission")]
    public async Task<IActionResult> RemovePermission(int roleId, int permissionId)
    {
        await _role.RemovePermissionAsync(roleId, permissionId);
        return Ok();
    }
    [HasPermission("role.view")]
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
        => Ok(await _role.GetRoleAsync(id));
    [HasPermission("role.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _role.GetAllAsync());
}
