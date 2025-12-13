using Microsoft.Extensions.DependencyInjection;
using Wms.Application.Interfaces.Services;
using Wms.Application.Services.Auth;

namespace Wms.Api.Extensions;

public static class AuthServiceRegistration
{
    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
