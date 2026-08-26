using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class PedidoProveedorDetalle
    {
        public int Id { get; set; }

        public int PedidoProveedorId { get; set; }

        public string CodigoProducto { get; set; }

        public string Color { get; set; }

        public string Talla { get; set; }

        public int Cantidad { get; set; }

        public string? Detalle { get; set; }

        public int? PedidoClienteId { get; set; }

        [ForeignKey(nameof(PedidoClienteId))]
        public PedidoCliente? PedidoCliente { get; set; }

        public int? NumeroPedidoCliente { get; set; }
    }
}
