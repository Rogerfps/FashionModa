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

        [Required]
        public int ProveedorCedula { get; set; }
        public Proveedor? Proveedor { get; set; }

        public ICollection<ImagenZapato> Imagenes { get; set; }
            = new List<ImagenZapato>();
    }
}

//Add-Migration ProveedorZapatos