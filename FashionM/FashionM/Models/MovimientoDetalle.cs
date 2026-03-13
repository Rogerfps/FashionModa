namespace FashionM.Models
{
    public class MovimientoDetalle
    {

        public int Id { get; set; }

        public int MovimientoInventarioId { get; set; }

        public string Numero { get; set; } = string.Empty;

        public string? Color { get; set; }

        public string? Detalle { get; set; }

        public int Cantidad { get; set; }

        public MovimientoInventario? MovimientoInventario { get; set; }
    }
}
