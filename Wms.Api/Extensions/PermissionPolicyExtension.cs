using Microsoft.Extensions.DependencyInjection;

namespace Wms.Api.Extensions;

public static class PermissionPolicyExtension
{
    public static IServiceCollection AddPermissionPolicies(this IServiceCollection services)
    {
        // Nếu muốn dùng policy-based
        // services.AddAuthorization(options =>
        // {
        //     options.AddPolicy("UserView", policy =>
        //         policy.RequireClaim("permission", "user.view"));
        // });

        return services;
    }
}
