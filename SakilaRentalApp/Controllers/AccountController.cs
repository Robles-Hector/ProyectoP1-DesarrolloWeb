using Microsoft.AspNetCore.Mvc;
using SakilaRentalApp.Data;
using SakilaRentalApp.Extensions;
using SakilaRentalApp.Models.ViewModels;

namespace SakilaRentalApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly UsuarioRepository _usuarioRepo;

        public AccountController(UsuarioRepository usuarioRepo)
        {
            _usuarioRepo = usuarioRepo;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (HttpContext.Session.EstaAutenticado())
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Validación de ModelState
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Reto 1: Autenticación mediante verificación de Hash y Salt
            var usuario = await _usuarioRepo.ValidarLoginAsync(model.Username, model.Password);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, "Nombre de usuario o contraseña incorrectos.");
                return View(model);
            }

            // Establecer variables en la sesión del usuario
            HttpContext.Session.SetInt32(SessionKeys.UsuarioId, usuario.UsuarioId);
            HttpContext.Session.SetString(SessionKeys.Username, usuario.Username);
            HttpContext.Session.SetString(SessionKeys.Rol, usuario.Rol);
            HttpContext.Session.SetString(SessionKeys.NombreCompleto, usuario.NombreCompleto);

            if (usuario.CustomerId.HasValue)
            {
                HttpContext.Session.SetInt32(SessionKeys.CustomerId, usuario.CustomerId.Value);
            }

            if (usuario.StaffId.HasValue)
            {
                HttpContext.Session.SetInt32(SessionKeys.StaffId, usuario.StaffId.Value);
            }

            TempData["SuccessMessage"] = $"¡Bienvenido/a al sistema, {usuario.NombreCompleto}! Rol: {usuario.Rol}.";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (HttpContext.Session.EstaAutenticado())
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (success, message, nuevoUsuario) = await _usuarioRepo.RegistrarClienteAsync(
                model.Username,
                model.Password,
                model.FirstName,
                model.LastName,
                model.Email
            );

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Su cuenta ha sido creada exitosamente. Inicie sesión para continuar.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "Ha cerrado sesión correctamente.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied(string? mensaje = null)
        {
            ViewBag.Mensaje = mensaje ?? "No posee los privilegios suficientes para acceder a esta sección del sistema.";
            return View();
        }
    }
}
