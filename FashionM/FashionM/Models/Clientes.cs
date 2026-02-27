using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class Clientes
    {
        [Key]
        [Required]
        public int Cedula { get; set; }

        [Required]
        [StringLength(50)]
        public string Codigo { get; set; } = string.Empty;

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
        public string Telefonos { get; set; } = string.Empty;

        public bool Estado { get; set; }

        [Required]
        public string Comercio { get; set; } = string.Empty;

        [Required]
        public string Direccion { get; set; } = string.Empty;

        [StringLength(100)]
        public string Agente { get; set; } = string.Empty;

        [StringLength(100)]
        public string Transporte { get; set; } = string.Empty;

        [Required]
        public decimal ActividadEconomica { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LimiteCredito { get; set; }

        [StringLength(50)]
        public string Zona { get; set; } = string.Empty;

        [StringLength(50)]
        public string Empresa { get; set; } = string.Empty;
    }
}


/*
Codigo
Agente
Transporte
Actividad economica
Limite de credito
Zona
Actualizar telefono para poner varios

[StringLength(50)]
        public string Empresa { get; set; } = string.Empty;
 */