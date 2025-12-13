using Microsoft.AspNetCore.Mvc;
using Wms.Api.Middlewares;
using Wms.Application.DTOS.Auth;
using Wms.Application.Interfaces.Services;

namespace Wms.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth)
    {
        _auth = auth;
    }

    // ================================
    // 🔵 USER CRUD
    // ================================

    // 📌 List all users
    [HttpGet]
    [HasPermission("user.view")]
    public async Task<IActionResult> GetAll()
        => Ok(await _auth.GetAllUsersAsync());


    // 📌 Get user by ID
    [HttpGet("{id}")]
    [HasPermission("user.view")]
    public async Task<IActionResult> GetById(int id)
        => Ok(await _auth.GetUserByIdAsync(id));


    // 📌 Create user (Admin)
    [HttpPost]
    [HasPermission("user.create")]
    public async Task<IActionResult> Create(CreateUserDto dto)
        => Ok(await _auth.CreateUserAsync(dto));

    // 📌 Update user
    [HttpPut("{id}")]
    [HasPermission("user.update")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        await _auth.UpdateUserAsync(id, dto);
        return Ok(new { message = "Updated" });
    }

    // 📌 Soft delete user
    [HttpDelete("{id}")]
    [HasPermission("user.delete")]
    public async Task<IActionResult> Delete(int id)
    {
        await _auth.DeleteUserAsync(id);
        return Ok(new { message = "Deleted" });
    }

    // ================================
    // 🔵 REGISTER / LOGIN (public)
    // ================================

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = await _auth.RegisterAsync(dto);
        return Ok(new { user.Id, user.FullName, user.Email });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
        => Ok(await _auth.LoginAsync(dto));


    // ================================
    // 🔵 Assign Role / Permission
    // ================================

    // 📌 Add Role to User
    [HttpPost("assign-role")]
    [HasPermission("user.assign-role")]
    public async Task<IActionResult> AssignRole(int userId, int roleId)
    {
        await _auth.AssignRoleAsync(userId, roleId);
        return Ok(new { Message = "Role assigned" });
    }


    // 📌 Add Permission to User
    [HttpPost("assign-permission")]
    [HasPermission("user.assign-permission")]
    public async Task<IActionResult> AssignPermission(int userId, int permissionId)
    {
        await _auth.AssignPermissionAsync(userId, permissionId);
        return Ok(new { Message = "Permission assigned" });
    }
}
