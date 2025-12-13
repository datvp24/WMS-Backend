using Wms.Application.DTOs.Auth;
using Wms.Application.DTOS.Auth;
using Wms.Domain.Entity.Auth;

namespace Wms.Application.Interfaces.Services;

public interface IAuthService
{
    Task<User> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<User> CreateUserAsync(CreateUserDto dto);
    Task UpdateUserAsync(int id, UpdateUserDto dto);
    Task DeleteUserAsync(int id);

    Task<List<UserDto>> GetAllUsersAsync();
    Task<UserDto> GetUserByIdAsync(int id);
    Task AssignRoleAsync(int userId, int roleId);
    Task AssignPermissionAsync(int userId, int permissionId);
}
