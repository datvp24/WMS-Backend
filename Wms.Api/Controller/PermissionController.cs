using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOs.System.Permissions;
using Wms.Application.Interfaces.Services.System;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PermissionController : ControllerBase
{
    private readonly IPermissionService _permission;

    public PermissionController(IPermissionService permission)
    {
        _permission = permission;
    }
    [HasPermission("permission.create")]
    [HttpPost]
    public async Task<IActionResult> Create(CreatePermissionDto dto)
        => Ok(await _permission.CreateAsync(dto));
    [HasPermission("permission.update")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdatePermissionDto dto)
    {
        await _permission.UpdateAsync(id, dto);
        return Ok();
    }
    [HasPermission("permission.delete")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _permission.DeleteAsync(id);
        return Ok();
    }
    [HasPermission("permission.view")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _permission.GetAllAsync());
}
