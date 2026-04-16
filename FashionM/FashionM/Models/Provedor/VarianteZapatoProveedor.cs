namespace FashionM.Models.Provedor
{
    public class VarianteZapatoProveedor
    {
        public int Id { get; set; }

        public string Color { get; set; } = string.Empty;

        public string Suela { get; set; } = string.Empty;

        public string Detalle { get; set; } = string.Empty;

        // Precios (NO varían por talla)
        public decimal PrecioVenta { get; set; }

        public decimal PrecioCosto { get; set; }

        public decimal CostoCOP { get; set; }

        // Relación
        public int ZapatoProveedorId { get; set; }
        public ZapatoProveedor Zapato { get; set; } = null!;

        public ICollection<TallaVarianteZapatoProveedor> Tallas { get; set; } = new List<TallaVarianteZapatoProveedor>();
    }
}
