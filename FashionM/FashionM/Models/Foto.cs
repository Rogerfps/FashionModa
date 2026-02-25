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
        public int InventarioId { get; set; }

        [ForeignKey(nameof(InventarioId))]
        public Inventario Inventario { get; set; }
    }
}
