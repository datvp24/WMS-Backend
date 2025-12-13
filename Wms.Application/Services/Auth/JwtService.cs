using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Wms.Application.DTOs.Auth;
using Wms.Application.DTOS.Auth;
using Wms.Application.Interfaces.Services;
using Wms.Domain.Entity.Auth;

namespace Wms.Application.Services.Auth;

public class JwtService : IJwtService
{
    private readonly IConfiguration _config;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtService(IConfiguration config, IHttpContextAccessor accessor)
    {
        _config = config;
        _httpContextAccessor = accessor;
    }

    public int? GetUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return null;

        var claim = httpContext.User.Claims.FirstOrDefault(x => x.Type == "uid");
        if (claim == null) return null;

        return int.Parse(claim.Value);
    }

    public AuthResponseDto GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new Claim("uid", user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"])
        );

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expireHours = int.Parse(_config["Jwt:ExpireHours"] ?? "4");

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expireHours),
            signingCredentials: creds
        );

        return new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            RefreshToken = GenerateRefreshToken(),
            ExpireAt = token.ValidTo
        };
    }

    public string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString().Replace("-", "");
    }
}
