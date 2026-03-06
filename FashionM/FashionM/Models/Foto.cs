using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class Foto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Ruta { get; set; }

        // FK
        public string? InventarioCodigo { get; set; }

        [ForeignKey(nameof(InventarioCodigo))]
        public Inventario? Inventario { get; set; }
    }
}
