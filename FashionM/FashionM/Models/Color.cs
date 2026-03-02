using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Color
    {
        [Key]
        public int IdColor { get; set; }

        [Required]
        public string NombreColor { get; set; } = string.Empty;

        public int CodigoId { get; set; }
        public Codigo Codigo { get; set; }

        // 🔴 OBLIGATORIO: plural y colección
        public ICollection<Suela> Suelas { get; set; } = new List<Suela>();
    }

}

