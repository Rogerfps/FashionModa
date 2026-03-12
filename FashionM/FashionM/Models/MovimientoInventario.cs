

namespace FashionM.Models
{
    public class MovimientoInventario
    {
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public string InventarioCodigo { get; set; } = string.Empty;

        public string Tipo { get; set; } = string.Empty; // Entrada / Salida

        public string Detalle { get; set; } = string.Empty;

        public Inventario? Inventario { get; set; }

        public List<MovimientoDetalle> Detalles { get; set; } = new();

    }
}
