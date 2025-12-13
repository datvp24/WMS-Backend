using Microsoft.EntityFrameworkCore;
using System;
using Wms.Application.DTOs.Auth;
using Wms.Application.DTOS.Auth;
using Wms.Application.Interfaces.Services;
using Wms.Domain.Entity.Auth;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Application.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;

    public AuthService(
        AppDbContext db,
        IPasswordHasher hasher,
        IJwtService jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<User> RegisterAsync(RegisterDto dto)
    {
        if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
            throw new Exception("Email already exists");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = _hasher.Hash(dto.Password)
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;


    }
    public async Task DeleteUserAsync(int id)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new Exception("User not found");

        user.IsDeleted = true;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _jwt.GetUserId();

        await _db.SaveChangesAsync();
    }
    public async Task<List<UserDto>> GetAllUsersAsync()
    {
        return await _db.Users
            .Where(x => !x.IsDeleted)
            .Select(x => new UserDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .ToListAsync();
    }
    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var u = await _db.Users
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new UserDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,
                CreatedBy = x.CreatedBy,
                UpdatedAt = x.UpdatedAt,
                UpdatedBy = x.UpdatedBy
            })
            .FirstOrDefaultAsync();

        if (u == null)
            throw new Exception("User not found");

        return u;
    }

    public async Task<User> CreateUserAsync(CreateUserDto dto)
    {
        if (await _db.Users.AnyAsync(x => x.Email == dto.Email))
            throw new Exception("Email already exists");

        var adminId = _jwt.GetUserId();

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PasswordHash = _hasher.Hash(dto.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = adminId
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();


        await _db.SaveChangesAsync();
        return user;
    }
    public async Task UpdateUserAsync(int id, UpdateUserDto dto)
    {
        var user = await _db.Users.FindAsync(id)
            ?? throw new Exception("User not found");

        user.FullName = dto.FullName;
        user.Email = dto.Email;
        user.IsActive = dto.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = _jwt.GetUserId();

        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = _hasher.Hash(dto.Password);




        await _db.SaveChangesAsync();
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email == dto.Email);

        if (user == null)
            throw new Exception("Invalid email or password");

        if (!_hasher.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid email or password");

        if (!user.IsActive)
            throw new Exception("User is deactivated");

        return _jwt.GenerateToken(user);
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        bool exists = await _db.UserRoles
            .AnyAsync(x => x.UserId == userId && x.RoleId == roleId);

        if (!exists)
        {
            _db.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });

            await _db.SaveChangesAsync();
        }
    }

    public async Task AssignPermissionAsync(int userId, int permissionId)
    {
        bool exists = await _db.UserPermissions
            .AnyAsync(x => x.UserId == userId && x.PermissionId == permissionId);

        if (!exists)
        {
            _db.UserPermissions.Add(new UserPermission
            {
                UserId = userId,
                PermissionId = permissionId
            });

            await _db.SaveChangesAsync();
        }
    }
}
