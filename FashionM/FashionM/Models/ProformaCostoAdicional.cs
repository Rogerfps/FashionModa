using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class ProformaCostoAdicional
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ProformaId { get; set; }

        public Proforma? Proforma { get; set; }

        [Required]
        [StringLength(150)]
        public string Descripcion { get; set; } = string.Empty;

        [Column(TypeName = "numeric(18,2)")]
        [Range(typeof(decimal), "0.01", "9999999999999999")]
        public decimal Monto { get; set; }
    }
}
