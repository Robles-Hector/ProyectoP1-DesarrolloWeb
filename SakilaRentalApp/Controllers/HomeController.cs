using Microsoft.AspNetCore.Mvc;
using SakilaRentalApp.Data;
using SakilaRentalApp.Extensions;

namespace SakilaRentalApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ReporteRepository _reporteRepo;
        private readonly FilmRepository _filmRepo;

        public HomeController(ReporteRepository reporteRepo, FilmRepository filmRepo)
        {
            _reporteRepo = reporteRepo;
            _filmRepo = filmRepo;
        }

        public async Task<IActionResult> Index()
        {
            var (totalPeliculas, totalAlquileres, alquileresActivos, totalClientes) = 
                await _reporteRepo.GetDashboardMetricsAsync();

            ViewBag.TotalPeliculas = totalPeliculas;
            ViewBag.TotalAlquileres = totalAlquileres;
            ViewBag.AlquileresActivos = alquileresActivos;
            ViewBag.TotalClientes = totalClientes;

            // Obtener 6 películas destacadas disponibles
            var (destacadas, _) = await _filmRepo.GetCatalogoAsync(
                disponibilidad: "disponibles", 
                incluirEliminadas: false, 
                pagina: 1, 
                tamanoPagina: 6
            );

            ViewBag.PeliculasDestacadas = destacadas;

            return View();
        }
    }
}
