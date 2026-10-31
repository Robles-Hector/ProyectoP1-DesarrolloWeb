using Microsoft.AspNetCore.Mvc;
using SakilaRentalApp.Data;
using SakilaRentalApp.Extensions;
using SakilaRentalApp.Models.ViewModels;

namespace SakilaRentalApp.Controllers
{
    public class PeliculasController : Controller
    {
        private readonly FilmRepository _filmRepo;
        private readonly InventoryRepository _inventoryRepo;

        public PeliculasController(FilmRepository filmRepo, InventoryRepository inventoryRepo)
        {
            _filmRepo = filmRepo;
            _inventoryRepo = inventoryRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? buscar = null,
            string? rating = null,
            string? disponibilidad = null,
            int pagina = 1)
        {
            if (pagina < 1) pagina = 1;
            const int pageSize = 12;

            bool esAdmin = HttpContext.Session.EsAdmin();

            // Usuarios regulares nunca ven películas eliminadas lógicamente
            var (peliculas, totalRegistros) = await _filmRepo.GetCatalogoAsync(
                buscar,
                rating,
                disponibilidad,
                incluirEliminadas: false,
                pagina: pagina,
                tamanoPagina: pageSize
            );

            var model = new FilmCatalogViewModel
            {
                Peliculas = peliculas,
                Buscar = buscar,
                Rating = rating,
                Disponibilidad = disponibilidad,
                PaginaActual = pagina,
                ElementosPorPagina = pageSize,
                TotalRegistros = totalRegistros
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Detalle(int id)
        {
            var film = await _filmRepo.GetByIdAsync(id);
            if (film == null || (film.IsDeleted && !HttpContext.Session.EsAdmin()))
            {
                TempData["ErrorMessage"] = "La película solicitada no existe o no se encuentra disponible.";
                return RedirectToAction("Index");
            }

            var copias = await _inventoryRepo.GetCopiesByFilmIdAsync(id);

            var model = new FilmDetailViewModel
            {
                Pelicula = film,
                CopiasInventario = copias,
                EsAdmin = HttpContext.Session.EsAdmin(),
                PuedeAlquilar = HttpContext.Session.EsCliente() && film.TieneDisponibilidad && !film.IsDeleted
            };

            return View(model);
        }
    }
}
