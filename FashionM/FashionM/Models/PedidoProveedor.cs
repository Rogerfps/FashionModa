namespace FashionM.Models
{
    public class PedidoProveedor
    {
        public int Id { get; set; }

        public int Semana { get; set; }

        public int PedidoMainId { get; set; }

        public string Empresa { get; set; }

        public int ProveedorCedula { get; set; }

        public Proveedor? Proveedor { get; set; }

        public DateTime FechaPedido { get; set; }

        public ICollection<PedidoProveedorDetalle> Detalles { get; set; }
        = new List<PedidoProveedorDetalle>();
    }
}
