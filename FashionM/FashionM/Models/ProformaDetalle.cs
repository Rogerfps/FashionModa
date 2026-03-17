using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class ProformaDetalle
    {
        [Key]
        public int Id { get; set; }

        public int ProformaId { get; set; }

        public Proforma? Proforma { get; set; }

        public string InventarioCodigo { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Talla { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal SubTotal { get; set; }
    }
}

