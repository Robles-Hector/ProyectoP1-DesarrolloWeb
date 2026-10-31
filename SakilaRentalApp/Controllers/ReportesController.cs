using Microsoft.AspNetCore.Mvc;
using SakilaRentalApp.Data;

namespace SakilaRentalApp.Controllers
{
    public class ReportesController : Controller
    {
        private readonly ReporteRepository _reporteRepo;

        public ReportesController(ReporteRepository reporteRepo)
        {
            _reporteRepo = reporteRepo;
        }

        /// <summary>
        /// Reporte solicitado: Reportes/PeliculasMasAlquiladas
        /// Ejecuta mediante ADO.NET la consulta parametrizada con TOP, INNER JOIN, COUNT, GROUP BY y ORDER BY.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PeliculasMasAlquiladas(int top = 10)
        {
            if (top < 5) top = 5;
            if (top > 50) top = 50;

            ViewBag.Top = top;
            var model = await _reporteRepo.GetPeliculasMasAlquiladasAsync(top);

            return View(model);
        }
    }
}
