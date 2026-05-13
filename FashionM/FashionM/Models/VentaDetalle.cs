using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class VentaDetalle
    {
        [Key]
        public int Id { get; set; }

        public int VentaId { get; set; }

        public Venta? Venta { get; set; }

        public string InventarioCodigo { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public string Talla { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal SubTotal { get; set; }

        public ICollection<NotaCreditoDetalle> NotasCreditoDetalle { get; set; }
            = new List<NotaCreditoDetalle>();
    }
}
