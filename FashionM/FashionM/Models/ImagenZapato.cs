using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class ImagenZapato
    {
        public int Id { get; set; }

        [Required]
        public int ZapatoId { get; set; }
        public Zapato Zapato { get; set; }

        [Required]
        public string Url { get; set; }
    }
}
