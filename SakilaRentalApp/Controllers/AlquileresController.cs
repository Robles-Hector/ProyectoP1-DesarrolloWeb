using Microsoft.AspNetCore.Mvc;
using SakilaRentalApp.Data;
using SakilaRentalApp.Extensions;
using SakilaRentalApp.Filters;

namespace SakilaRentalApp.Controllers
{
    public class AlquileresController : Controller
    {
        private readonly RentalRepository _rentalRepo;
        private readonly FilmRepository _filmRepo;

        public AlquileresController(RentalRepository rentalRepo, FilmRepository filmRepo)
        {
            _rentalRepo = rentalRepo;
            _filmRepo = filmRepo;
        }

        /// <summary>
        /// Proceso de Alquiler:
        /// Regla de negocio: El cliente NO selecciona manualmente su ID. Se obtiene directamente de la Session.
        /// Regla de negocio: Disponibilidad verificada en tiempo real.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole("Cliente")]
        public async Task<IActionResult> Alquilar(int filmId)
        {
            int? customerId = HttpContext.Session.GetCustomerId();

            if (!customerId.HasValue)
            {
                TempData["ErrorMessage"] = "Su usuario no tiene un cliente de Sakila asociado. Por favor contacte al administrador.";
                return RedirectToAction("Detalle", "Peliculas", new { id = filmId });
            }

            byte staffId = HttpContext.Session.GetStaffId() ?? 1;

            var (success, message, rentalId) = await _rentalRepo.AlquilarPeliculaAsync(
                filmId,
                customerId.Value,
                staffId
            );

            if (success)
            {
                TempData["SuccessMessage"] = message;
                return RedirectToAction(nameof(MisAlquileres));
            }
            else
            {
                TempData["ErrorMessage"] = message;
                return RedirectToAction("Detalle", "Peliculas", new { id = filmId });
            }
        }

        /// <summary>
        /// Mis Alquileres:
        /// Muestra únicamente los alquileres pertenecientes al cliente autenticado en la Session actual.
        /// </summary>
        [HttpGet]
        [AuthorizeRole("Cliente")]
        public async Task<IActionResult> MisAlquileres()
        {
            int? customerId = HttpContext.Session.GetCustomerId();
            if (!customerId.HasValue)
            {
                TempData["ErrorMessage"] = "No se encontró el identificador del cliente en su sesión.";
                return RedirectToAction("Index", "Home");
            }

            var alquileres = await _rentalRepo.GetAlquileresPorClienteAsync(customerId.Value);
            return View(alquileres);
        }

        /// <summary>
        /// Devolución de Película:
        /// Regla de negocio: Un cliente solo puede devolver alquileres propios.
        /// Regla de negocio: Un alquiler ya devuelto no puede volver a devolverse.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRole] // Tanto Cliente como Admin pueden devolver, pero el repo valida propiedad
        public async Task<IActionResult> Devolver(int rentalId, string? returnUrl = null)
        {
            bool esAdmin = HttpContext.Session.EsAdmin();
            int? customerId = HttpContext.Session.GetCustomerId();

            var (success, message) = await _rentalRepo.DevolverPeliculaAsync(
                rentalId,
                customerId,
                esAdmin
            );

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            if (esAdmin)
            {
                return RedirectToAction(nameof(HistorialAdmin));
            }

            return RedirectToAction(nameof(MisAlquileres));
        }

        /// <summary>
        /// Vista de administración de alquileres:
        /// Consulta global de los alquileres realizados en el videoclub.
        /// </summary>
        [HttpGet]
        [AuthorizeRole("Administrador")]
        public async Task<IActionResult> HistorialAdmin(string? buscarCliente = null, bool soloPendientes = false)
        {
            ViewBag.BuscarCliente = buscarCliente;
            ViewBag.SoloPendientes = soloPendientes;

            var rentals = await _rentalRepo.GetAllAlquileresAdminAsync(buscarCliente, soloPendientes);
            return View(rentals);
        }
    }
}
