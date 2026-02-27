using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class PedidoClienteDetalle
    {
        [Key]
        public int Id { get; set; }

        public int PedidoClienteId { get; set; }

        [ForeignKey(nameof(PedidoClienteId))]
        public PedidoCliente PedidoCliente { get; set; } = null!;

        public string CodigoProducto { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Talla { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }

        [NotMapped]
        public decimal SubTotal => Cantidad * PrecioUnitario;
    }

}

