using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.System.Permissions;
using Wms.Application.DTOs.System.Roles;
using Wms.Application.Interfaces.Services.System;
using Wms.Domain.Entity.Auth;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.System;

public class RoleService : IRoleService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;


    public RoleService(AppDbContext db, IHttpContextAccessor http)
    {
        _http = http;
        _db = db;
    }

    private int GetUserId()
    {
        return int.Parse(_http.HttpContext!.User.FindFirst("uid")!.Value);
    }
    public async Task<int> CreateRoleAsync(CreateRoleDto dto)
    {
        var userid = GetUserId();
        var role = new Role
        {
            RoleName = dto.RoleName,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userid
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();
        return role.Id;
    }


    public async Task UpdateRoleAsync(int id, UpdateRoleDto dto)
    {
        var userid = GetUserId();

        var role = await _db.Roles.FindAsync(id)
            ?? throw new Exception("Role not found");
        var rolecre = role;


        role.RoleName = dto.RoleName;
        role.UpdatedAt = DateTime.UtcNow;
        role.UpdatedBy = userid;
        role.CreatedAt = rolecre.CreatedAt;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteRoleAsync(int id)
    {
        var role = await _db.Roles.FindAsync(id)
            ?? throw new Exception("Role not found");

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
    }

    public async Task AssignPermissionAsync(AssignPermissionToRoleDto dto)
    {
        bool exists = await _db.RolePermissions
            .AnyAsync(x => x.RoleId == dto.RoleId && x.PermissionId == dto.PermissionId);

        if (!exists)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                RoleId = dto.RoleId,
                PermissionId = dto.PermissionId
            });
            await _db.SaveChangesAsync();
        }
    }

    public async Task RemovePermissionAsync(int roleId, int permissionId)
    {
        var rp = await _db.RolePermissions
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.PermissionId == permissionId)
            ?? throw new Exception("Permission not assigned to this role");

        _db.RolePermissions.Remove(rp);
        await _db.SaveChangesAsync();
    }

    public async Task<RoleDetailDto> GetRoleAsync(int id)
    {
        var role = await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new Exception("Role not found");

        return new RoleDetailDto
        {
            Id = role.Id,
            RoleName = role.RoleName,
            Permissions = role.RolePermissions
                .Select(p => new PermissionDto
                {
                    Id = p.PermissionId,
                    Code = p.Permission.Code,
                    Description = p.Permission.Description
                }).ToList(),
            CreatedAt = role.CreatedAt,
            CreatedBy = role.CreatedBy,
            UpdatedAt = role.UpdatedAt,
            UpdatedBy = role.UpdatedBy
        };
    }

    public async Task<List<RoleDetailDto>> GetAllAsync()
    {
        return await _db.Roles
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .Select(role => new RoleDetailDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Permissions = role.RolePermissions
                    .Select(p => new PermissionDto
                    {
                        Id = p.Permission.Id,
                        Code = p.Permission.Code,
                        Description = p.Permission.Description
                    }).ToList(),
                CreatedAt = role.CreatedAt,
                CreatedBy = role.CreatedBy,
                UpdatedAt = role.UpdatedAt,
                UpdatedBy = role.UpdatedBy
            }).ToListAsync();
    }

}
