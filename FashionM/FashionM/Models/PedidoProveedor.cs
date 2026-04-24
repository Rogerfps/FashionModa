using FashionM.Models.Provedor;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class PedidoProveedor
    {
        public int Id { get; set; }

        public int Semana { get; set; }

        public int PedidoMainId { get; set; }

        public string Empresa { get; set; }

        public int ProveedorCatalogoId { get; set; }

        [ForeignKey("ProveedorCatalogoId")]
        public ProveedorCatalogo Proveedor { get; set; }

        public DateTime FechaPedido { get; set; }

        public ICollection<PedidoProveedorDetalle> Detalles { get; set; }
        = new List<PedidoProveedorDetalle>();
    }
}
