using System.ComponentModel.DataAnnotations;

namespace SakilaRentalApp.Models
{
    public class Film
    {
        public int FilmId { get; set; }

        [Required(ErrorMessage = "El título de la película es obligatorio.")]
        [StringLength(255, ErrorMessage = "El título no puede superar los 255 caracteres.")]
        [Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [RegularExpression(@"^(19|20)\d{2}$", ErrorMessage = "El año debe tener 4 dígitos válidos (ej. 2006).")]
        [Display(Name = "Año de Estreno")]
        public string? ReleaseYear { get; set; }

        [Required(ErrorMessage = "El idioma es obligatorio.")]
        [Display(Name = "Idioma")]
        public byte LanguageId { get; set; } = 1;

        [Display(Name = "Idioma")]
        public string? LanguageName { get; set; }

        public byte? OriginalLanguageId { get; set; }

        [Required(ErrorMessage = "La duración de renta es obligatoria.")]
        [Range(1, 30, ErrorMessage = "La duración del alquiler debe ser entre 1 y 30 días.")]
        [Display(Name = "Duración Alquiler (días)")]
        public byte RentalDuration { get; set; } = 3;

        [Required(ErrorMessage = "La tarifa de alquiler es obligatoria.")]
        [Range(0.01, 99.99, ErrorMessage = "La tarifa debe ser mayor a 0 y menor a 100.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Tarifa de Alquiler ($)")]
        public decimal RentalRate { get; set; } = 4.99m;

        [Range(1, 1000, ErrorMessage = "La duración debe ser entre 1 y 1000 minutos.")]
        [Display(Name = "Duración (minutos)")]
        public short? Length { get; set; }

        [Required(ErrorMessage = "El costo de reemplazo es obligatorio.")]
        [Range(0.01, 999.99, ErrorMessage = "El costo de reemplazo debe ser mayor a 0.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Costo de Reemplazo ($)")]
        public decimal ReplacementCost { get; set; } = 19.99m;

        [Required(ErrorMessage = "La clasificación por edades es obligatoria.")]
        [Display(Name = "Clasificación (Rating)")]
        public string Rating { get; set; } = "PG";

        [Display(Name = "Características Especiales")]
        public string? SpecialFeatures { get; set; }

        [Display(Name = "Eliminado Lógicamente")]
        public bool IsDeleted { get; set; } = false;

        [Display(Name = "Última Actualización")]
        public DateTime LastUpdate { get; set; } = DateTime.Now;

        // Propiedades calculadas a partir del inventario y alquileres
        [Display(Name = "Copias Totales")]
        public int TotalCopias { get; set; }

        [Display(Name = "Copias Disponibles")]
        public int CopiasDisponibles { get; set; }

        [Display(Name = "Copias Alquiladas")]
        public int CopiasAlquiladas => TotalCopias - CopiasDisponibles;

        public bool TieneDisponibilidad => CopiasDisponibles > 0;
    }
}
