using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace CafeManagement.API.Services;
public class StaffAccessFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        if (controller == "PublicMenu" || controller == "Auth" && action == "Login") return;
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true) { context.Result = new UnauthorizedResult(); return; }
        if (controller == "Auth" && action == "Me") return;
        if (user.IsInRole("Admin")) return;
        var required = DynamicAccess.Required(controller, action, context.HttpContext.Request.Method);
        if (!required.Any(code => DynamicAccess.Has(user, code))) context.Result = new ForbidResult();
    }
}
