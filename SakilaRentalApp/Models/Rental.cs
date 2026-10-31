using System.ComponentModel.DataAnnotations;

namespace SakilaRentalApp.Models
{
    public class Rental
    {
        [Display(Name = "ID Alquiler")]
        public int RentalId { get; set; }

        [Display(Name = "Fecha de Alquiler")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime RentalDate { get; set; }

        [Display(Name = "ID Inventario")]
        public int InventoryId { get; set; }

        [Display(Name = "ID Película")]
        public int FilmId { get; set; }

        [Display(Name = "Película")]
        public string FilmTitle { get; set; } = string.Empty;

        [Display(Name = "Tarifa")]
        public decimal RentalRate { get; set; }

        [Display(Name = "ID Cliente")]
        public int CustomerId { get; set; }

        [Display(Name = "Cliente")]
        public string CustomerName { get; set; } = string.Empty;

        [Display(Name = "Fecha de Devolución")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}", NullDisplayText = "Pendiente")]
        public DateTime? ReturnDate { get; set; }

        [Display(Name = "ID Empleado")]
        public byte StaffId { get; set; }

        [Display(Name = "Atendido por")]
        public string? StaffName { get; set; }

        [Display(Name = "Última Actualización")]
        public DateTime LastUpdate { get; set; }

        public bool EstaDevuelto => ReturnDate.HasValue;

        public int DiasTranscurridos => (DateTime.Now - RentalDate).Days;
    }
}
