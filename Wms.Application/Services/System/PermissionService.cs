using Microsoft.EntityFrameworkCore;
using Wms.Application.DTOs.System.Permissions;
using Wms.Application.Interfaces.Services.System;
using Wms.Domain.Entity.Auth;
using Wms.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;


namespace Wms.Application.Services.System;

public class PermissionService : IPermissionService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public PermissionService(AppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    private int GetUserId()
    {
        return int.Parse(_http.HttpContext!.User.FindFirst("uid")!.Value);
    }

    public async Task<int> CreateAsync(CreatePermissionDto dto)
    {
        var userId = GetUserId();

        var p = new Permission
        {
            Code = dto.Code,
            Description = dto.Description,

            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,

            IsDeleted = false
        };

        _db.Permissions.Add(p);
        await _db.SaveChangesAsync();
        return p.Id;
    }


    public async Task UpdateAsync(int id, UpdatePermissionDto dto)
    {
        var userId = GetUserId();

        var p = await _db.Permissions.FindAsync(id)
            ?? throw new Exception("Permission not found");

        p.Description = dto.Description;
        p.Code = dto.Code;

        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userId;

        await _db.SaveChangesAsync();
    }


    public async Task DeleteAsync(int id)
    {
        var userId = GetUserId();

        var p = await _db.Permissions.FindAsync(id)
            ?? throw new Exception("Permission not found");

        p.IsDeleted = true;
        p.UpdatedAt = DateTime.UtcNow;
        p.UpdatedBy = userId;

        await _db.SaveChangesAsync();
    }


    public async Task<List<PermissionDto>> GetAllAsync()
    {
        return await _db.Permissions
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                CreatedBy= p.CreatedBy
            }).ToListAsync();
    }
}
