using Microsoft.AspNetCore.Mvc.Rendering;

namespace SakilaRentalApp.Models.ViewModels
{
    public class FilmCatalogViewModel
    {
        public List<Film> Peliculas { get; set; } = new();

        public string? Buscar { get; set; }
        public string? Rating { get; set; }
        public string? Disponibilidad { get; set; } // "todas", "disponibles", "agotadas"
        public bool? MostrarEliminadas { get; set; } // Solo para Admin

        public int PaginaActual { get; set; } = 1;
        public int ElementosPorPagina { get; set; } = 12;
        public int TotalRegistros { get; set; }

        public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / ElementosPorPagina);

        public List<SelectListItem> Ratings { get; set; } = new()
        {
            new SelectListItem { Value = "", Text = "Todas las clasificaciones" },
            new SelectListItem { Value = "G", Text = "G" },
            new SelectListItem { Value = "PG", Text = "PG" },
            new SelectListItem { Value = "PG-13", Text = "PG-13" },
            new SelectListItem { Value = "R", Text = "R" },
            new SelectListItem { Value = "NC-17", Text = "NC-17" }
        };
    }

    public class FilmDetailViewModel
    {
        public Film Pelicula { get; set; } = new();
        public List<InventoryItem> CopiasInventario { get; set; } = new();
        public bool EsAdmin { get; set; }
        public bool PuedeAlquilar { get; set; }
    }
}
