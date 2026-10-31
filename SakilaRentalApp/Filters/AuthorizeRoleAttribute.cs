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
                string returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
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
                    // Redirigir a pantalla de acceso denegado si intenta acceder a una URL restringida
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
