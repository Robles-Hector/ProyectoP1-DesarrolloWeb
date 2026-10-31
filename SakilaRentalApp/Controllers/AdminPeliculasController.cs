using Microsoft.AspNetCore.Mvc;
using SakilaRentalApp.Data;
using SakilaRentalApp.Filters;
using SakilaRentalApp.Models;
using SakilaRentalApp.Models.ViewModels;

namespace SakilaRentalApp.Controllers
{
    [AuthorizeRole("Administrador")]
    public class AdminPeliculasController : Controller
    {
        private readonly FilmRepository _filmRepo;
        private readonly InventoryRepository _inventoryRepo;

        public AdminPeliculasController(FilmRepository filmRepo, InventoryRepository inventoryRepo)
        {
            _filmRepo = filmRepo;
            _inventoryRepo = inventoryRepo;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string? buscar = null,
            string? rating = null,
            bool mostrarEliminadas = false,
            int pagina = 1)
        {
            if (pagina < 1) pagina = 1;
            const int pageSize = 15;

            var (peliculas, totalRegistros) = await _filmRepo.GetCatalogoAsync(
                buscar,
                rating,
                disponibilidad: null,
                incluirEliminadas: mostrarEliminadas,
                pagina: pagina,
                tamanoPagina: pageSize
            );

            var model = new FilmCatalogViewModel
            {
                Peliculas = peliculas,
                Buscar = buscar,
                Rating = rating,
                MostrarEliminadas = mostrarEliminadas,
                PaginaActual = pagina,
                ElementosPorPagina = pageSize,
                TotalRegistros = totalRegistros
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Crear()
        {
            var model = new FilmFormViewModel
            {
                Idiomas = await _filmRepo.GetLanguagesAsync(),
                RentalRate = 4.99m,
                RentalDuration = 3,
                ReplacementCost = 19.99m,
                Rating = "PG"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(FilmFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Idiomas = await _filmRepo.GetLanguagesAsync();
                return View(model);
            }

            var film = new Film
            {
                Title = model.Title,
                Description = model.Description,
                ReleaseYear = model.ReleaseYear,
                LanguageId = model.LanguageId,
                RentalDuration = model.RentalDuration,
                RentalRate = model.RentalRate,
                Length = model.Length,
                ReplacementCost = model.ReplacementCost,
                Rating = model.Rating,
                SpecialFeatures = model.SpecialFeatures,
                IsDeleted = model.IsDeleted
            };

            int newId = await _filmRepo.CreateAsync(film);
            if (newId > 0)
            {
                // Por defecto creamos 2 copias en inventario para que la película pueda alquilarse
                await _inventoryRepo.AddCopyAsync(newId, storeId: 1);
                await _inventoryRepo.AddCopyAsync(newId, storeId: 2);

                TempData["SuccessMessage"] = $"Película '{film.Title}' creada exitosamente con ID {newId} y 2 copias iniciales en inventario.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Ocurrió un error al registrar la película en la base de datos.");
            model.Idiomas = await _filmRepo.GetLanguagesAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Editar(int id)
        {
            var film = await _filmRepo.GetByIdAsync(id);
            if (film == null)
            {
                TempData["ErrorMessage"] = "La película solicitada no existe.";
                return RedirectToAction(nameof(Index));
            }

            var model = new FilmFormViewModel
            {
                FilmId = film.FilmId,
                Title = film.Title,
                Description = film.Description,
                ReleaseYear = film.ReleaseYear,
                LanguageId = film.LanguageId,
                RentalDuration = film.RentalDuration,
                RentalRate = film.RentalRate,
                Length = film.Length,
                ReplacementCost = film.ReplacementCost,
                Rating = film.Rating,
                SpecialFeatures = film.SpecialFeatures,
                IsDeleted = film.IsDeleted,
                Idiomas = await _filmRepo.GetLanguagesAsync()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(FilmFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Idiomas = await _filmRepo.GetLanguagesAsync();
                return View(model);
            }

            var film = new Film
            {
                FilmId = model.FilmId,
                Title = model.Title,
                Description = model.Description,
                ReleaseYear = model.ReleaseYear,
                LanguageId = model.LanguageId,
                RentalDuration = model.RentalDuration,
                RentalRate = model.RentalRate,
                Length = model.Length,
                ReplacementCost = model.ReplacementCost,
                Rating = model.Rating,
                SpecialFeatures = model.SpecialFeatures,
                IsDeleted = model.IsDeleted
            };

            bool updated = await _filmRepo.UpdateAsync(film);
            if (updated)
            {
                TempData["SuccessMessage"] = $"Película '{film.Title}' actualizada satisfactoriamente.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Error al actualizar la película.");
            model.Idiomas = await _filmRepo.GetLanguagesAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDelete(int id, string? returnUrl = null)
        {
            var film = await _filmRepo.GetByIdAsync(id);
            if (film == null)
            {
                TempData["ErrorMessage"] = "Película no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            bool success = await _filmRepo.ToggleSoftDeleteAsync(id);
            if (success)
            {
                string estado = !film.IsDeleted ? "eliminada lógicamente (desactivada)" : "reactivada";
                TempData["SuccessMessage"] = $"Película '{film.Title}' ha sido {estado} correctamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se pudo cambiar el estado de la película.";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Inventario(int id)
        {
            var film = await _filmRepo.GetByIdAsync(id);
            if (film == null)
            {
                TempData["ErrorMessage"] = "Película no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            var copias = await _inventoryRepo.GetCopiesByFilmIdAsync(id);

            ViewBag.Film = film;
            return View(copias);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AgregarCopia(int filmId, int storeId = 1)
        {
            var film = await _filmRepo.GetByIdAsync(filmId);
            if (film == null)
            {
                TempData["ErrorMessage"] = "Película no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            int newInventoryId = await _inventoryRepo.AddCopyAsync(filmId, storeId);
            if (newInventoryId > 0)
            {
                TempData["SuccessMessage"] = $"Nueva copia #{newInventoryId} agregada al inventario de la Tienda {storeId} para '{film.Title}'.";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al registrar la copia en inventario.";
            }

            return RedirectToAction(nameof(Inventario), new { id = filmId });
        }
    }
}
