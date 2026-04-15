using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class Zapato
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        public string Color { get; set; } = string.Empty;

        [Required]
        public string Suela { get; set; } = string.Empty;

        [Required]
        public string Numero { get; set; } = string.Empty; // talla
        public string? Detalle { get; set; }

        public int Cantidad { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCosto { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; } = 0;

        public bool Activo { get; set; } = true;

        public string Empresa { get; set; }

        [Required]
        public int ProveedorCedula { get; set; }
        public Proveedor? Proveedor { get; set; }

        public ICollection<ImagenZapato> Imagenes { get; set; }
            = new List<ImagenZapato>();
    }
}

//Add-Migration ProveedorZapatos