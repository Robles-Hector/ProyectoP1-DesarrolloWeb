using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SakilaRentalApp.Models.ViewModels
{
    public class FilmFormViewModel
    {
        public int FilmId { get; set; }

        [Required(ErrorMessage = "El título de la película es obligatorio.")]
        [StringLength(255, ErrorMessage = "El título no puede exceder 255 caracteres.")]
        [Display(Name = "Título")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [RegularExpression(@"^(19|20)\d{2}$", ErrorMessage = "El año de estreno debe ser un año válido de 4 dígitos (ej. 2006).")]
        [Display(Name = "Año de Estreno")]
        public string? ReleaseYear { get; set; } = DateTime.Now.Year.ToString();

        [Required(ErrorMessage = "Debe seleccionar un idioma.")]
        [Display(Name = "Idioma")]
        public byte LanguageId { get; set; } = 1;

        [Required(ErrorMessage = "La duración de renta es requerida.")]
        [Range(1, 30, ErrorMessage = "La duración del alquiler debe ser entre 1 y 30 días.")]
        [Display(Name = "Duración Alquiler (días)")]
        public byte RentalDuration { get; set; } = 3;

        [Required(ErrorMessage = "La tarifa de alquiler es requerida.")]
        [Range(0.01, 99.99, ErrorMessage = "La tarifa de alquiler debe ser mayor a 0 y menor a 100.")]
        [Display(Name = "Tarifa de Alquiler ($)")]
        public decimal RentalRate { get; set; } = 4.99m;

        [Range(1, 1000, ErrorMessage = "La duración debe estar entre 1 y 1000 minutos.")]
        [Display(Name = "Duración (minutos)")]
        public short? Length { get; set; } = 90;

        [Required(ErrorMessage = "El costo de reemplazo es requerido.")]
        [Range(0.01, 999.99, ErrorMessage = "El costo de reemplazo debe ser mayor a 0.")]
        [Display(Name = "Costo de Reemplazo ($)")]
        public decimal ReplacementCost { get; set; } = 19.99m;

        [Required(ErrorMessage = "Debe seleccionar una clasificación.")]
        [Display(Name = "Clasificación (Rating)")]
        public string Rating { get; set; } = "PG";

        [Display(Name = "Características Especiales")]
        public string? SpecialFeatures { get; set; }

        [Display(Name = "¿Eliminado Lógicamente?")]
        public bool IsDeleted { get; set; } = false;

        // Opciones para selects en la vista
        public List<SelectListItem> Idiomas { get; set; } = new();
        public List<SelectListItem> Ratings { get; set; } = new()
        {
            new SelectListItem { Value = "G", Text = "G (Todo Público)" },
            new SelectListItem { Value = "PG", Text = "PG (Guía Paternal Sugerida)" },
            new SelectListItem { Value = "PG-13", Text = "PG-13 (Mayores de 13 años)" },
            new SelectListItem { Value = "R", Text = "R (Restringido)" },
            new SelectListItem { Value = "NC-17", Text = "NC-17 (Adultos Únicamente)" }
        };
    }
}
