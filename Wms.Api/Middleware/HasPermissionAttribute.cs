using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Wms.Infrastructure.Persistence.Context;

namespace Wms.Api.Middlewares;

[AttributeUsage(AttributeTargets.Method)]
public class HasPermissionAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _permission;

    public HasPermissionAttribute(string permission)
    {
        _permission = permission;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var http = context.HttpContext;
        var db = http.RequestServices.GetRequiredService<AppDbContext>();

        var userId = http.User.FindFirst("uid")?.Value;

        if (userId == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        int uid = int.Parse(userId);

        bool hasPermission =
            await db.UserPermissions.AnyAsync(x => x.UserId == uid && x.Permission.Code == _permission)
            ||
            await (
                from ur in db.UserRoles
                join rp in db.RolePermissions on ur.RoleId equals rp.RoleId
                join p in db.Permissions on rp.PermissionId equals p.Id
                where ur.UserId == uid && p.Code == _permission
                select p
            ).AnyAsync();

        if (!hasPermission)
            context.Result = new ForbidResult();
    }
}
