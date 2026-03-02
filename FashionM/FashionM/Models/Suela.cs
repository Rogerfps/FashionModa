using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Suela
    {
        [Key]
        public int IdSuela { get; set; }

        [Required]
        public string TipoSuela { get; set; } = string.Empty;

        // FK
        public int ColorId { get; set; }
        public Color Color { get; set; }
    }
}
