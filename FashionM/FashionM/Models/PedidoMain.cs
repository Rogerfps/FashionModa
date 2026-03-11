namespace FashionM.Models
{
    public class PedidoMain
    {
        public int Id { get; set; }

        public int Semana { get; set; }

        public DateTime FechaGenerado { get; set; }

        public ICollection<PedidoProveedor> PedidosProveedor { get; set; }
    }
}

