using System.ComponentModel.DataAnnotations;

namespace SakilaRentalApp.Models
{
    public class InventoryItem
    {
        [Display(Name = "ID Inventario")]
        public int InventoryId { get; set; }

        public int FilmId { get; set; }

        [Display(Name = "Título")]
        public string FilmTitle { get; set; } = string.Empty;

        [Display(Name = "Tienda")]
        public int StoreId { get; set; }

        [Display(Name = "Última Actualización")]
        public DateTime LastUpdate { get; set; }

        [Display(Name = "Alquilada")]
        public bool EstaAlquilada { get; set; }

        [Display(Name = "ID Alquiler Actual")]
        public int? CurrentRentalId { get; set; }

        [Display(Name = "Cliente Actual")]
        public string? CurrentCustomerName { get; set; }

        [Display(Name = "Fecha Alquiler Actual")]
        public DateTime? CurrentRentalDate { get; set; }
    }
}
