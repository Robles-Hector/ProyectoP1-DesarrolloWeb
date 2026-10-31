using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SakilaRentalApp.Extensions;

namespace SakilaRentalApp.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _allowedRoles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _allowedRoles = roles ?? Array.Empty<string>();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var session = context.HttpContext.Session;

            // 1. Verificar si el usuario ha iniciado sesión
            if (!session.EstaAutenticado())
            {
                string returnUrl;
                if (HttpMethods.IsGet(context.HttpContext.Request.Method))
                {
                    returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                }
                else
                {
                    // Para peticiones POST/DELETE, redirigir al origen (Referer) o al inicio para evitar 404
                    string? referer = context.HttpContext.Request.Headers.Referer;
                    returnUrl = !string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.RelativeOrAbsolute, out var uri)
                        ? (uri.IsAbsoluteUri ? uri.PathAndQuery : uri.ToString())
                        : "/";
                }

                context.Result = new RedirectToActionResult("Login", "Account", new { returnUrl });
                return;
            }

            // 2. Si se especificaron roles permitidos, verificar que el rol del usuario coincida
            if (_allowedRoles.Length > 0)
            {
                string? rolUsuario = session.GetRol();

                bool tienePermiso = _allowedRoles.Any(r => 
                    string.Equals(r, rolUsuario, StringComparison.OrdinalIgnoreCase));

                if (!tienePermiso)
                {
                    context.Result = new RedirectToActionResult("AccessDenied", "Account", new 
                    { 
                        mensaje = $"Acceso restringido: Se requiere rol {string.Join(" o ", _allowedRoles)} para realizar esta operación." 
                    });
                    return;
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
