using System.ComponentModel.DataAnnotations;

namespace SakilaRentalApp.Models
{
    public class Usuario
    {
        public int UsuarioId { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [StringLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres.")]
        [Display(Name = "Usuario")]
        public string Username { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Salt { get; set; } = string.Empty;

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [StringLength(20)]
        [Display(Name = "Rol")]
        public string Rol { get; set; } = "Cliente"; // 'Administrador' o 'Cliente'

        [Required(ErrorMessage = "El nombre completo es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "El correo electrónico no tiene un formato válido.")]
        [StringLength(100)]
        [Display(Name = "Correo Electrónico")]
        public string? Email { get; set; }

        [Display(Name = "Cliente Sakila")]
        public int? CustomerId { get; set; }

        [Display(Name = "Empleado Sakila")]
        public byte? StaffId { get; set; }

        [Display(Name = "Activo")]
        public bool Activo { get; set; } = true;

        [Display(Name = "Fecha de Creación")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
