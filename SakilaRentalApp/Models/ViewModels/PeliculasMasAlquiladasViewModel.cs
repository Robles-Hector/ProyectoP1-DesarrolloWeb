using System.ComponentModel.DataAnnotations;

namespace SakilaRentalApp.Models.ViewModels
{
    public class PeliculasMasAlquiladasViewModel
    {
        [Display(Name = "ID")]
        public int FilmId { get; set; }

        [Display(Name = "Película")]
        public string Titulo { get; set; } = string.Empty;

        [Display(Name = "Cantidad de Alquileres")]
        public int CantidadAlquileres { get; set; }

        [Display(Name = "Clasificación")]
        public string Rating { get; set; } = string.Empty;

        [Display(Name = "Año de Estreno")]
        public string? ReleaseYear { get; set; }

        [Display(Name = "Tarifa ($)")]
        public decimal RentalRate { get; set; }

        [Display(Name = "Copias en Inventario")]
        public int CopiasInventario { get; set; }

        [Display(Name = "Copias Disponibles Hoy")]
        public int CopiasDisponibles { get; set; }
    }
}
