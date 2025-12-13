using Wms.Application.DTOs.Auth;
using Wms.Application.DTOS.Auth;
using Wms.Domain.Entity.Auth;

namespace Wms.Application.Interfaces.Services;

public interface IJwtService
{
    AuthResponseDto GenerateToken(User user);
    int? GetUserId();
    string GenerateRefreshToken();
}
