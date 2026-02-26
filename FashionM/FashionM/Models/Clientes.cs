using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Clientes
    {
        [Key]
        [Required]
        public int Cedula { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        public string IDTipo { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public int Telefono { get; set; }

        public bool Estado { get; set; }

        [Required]
        public string Comercio { get; set; } = string.Empty;

        [Required]
        public string Direccion { get; set; } = string.Empty;
    }
}
