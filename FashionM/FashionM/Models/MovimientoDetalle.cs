namespace FashionM.Models
{
    public class MovimientoDetalle
    {

        public int Id { get; set; }

        public int MovimientoInventarioId { get; set; }

        public string Numero { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        public MovimientoInventario? MovimientoInventario { get; set; }
    }
}
