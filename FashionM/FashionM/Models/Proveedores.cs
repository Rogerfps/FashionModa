using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Proveedor
    {
        [Key]
        public int Cedula { get; set; }

        [Required, StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Apellidos { get; set; } = string.Empty;

        [Required]
        public string IDTipo { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Correo { get; set; } = string.Empty;

        [Required]
        public int Telefono { get; set; }

        public bool Estado { get; set; }

        [Required]
        public string Comercio { get; set; } = string.Empty;

        [Required]
        public string Direccion { get; set; } = string.Empty;

        [Required]
        public decimal Actividad { get; set; }

        // 🔗 Relación
        public ICollection<Zapato> Zapatos { get; set; } = new List<Zapato>();
    }
}

/*
 
 Necesito actualizar un modulo de un sistema en .netcore con para que funcione con el modulo de pedidos
 Un provedor puede tener varios codigos y varios codigos puden tener varios colores y suelas

 */