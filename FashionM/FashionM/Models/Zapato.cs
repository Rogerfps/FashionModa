using System.ComponentModel.DataAnnotations;

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

        // FK Proveedor
        [Required]
        public int ProveedorCedula { get; set; }
        public Proveedor? Proveedor { get; set; }
    }
}

//Add-Migration ProveedorZapatos