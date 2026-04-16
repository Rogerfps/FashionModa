namespace FashionM.Models.Provedor
{
    public class TallaVarianteZapatoProveedor
    {
        public int Id { get; set; }

        public int Numero { get; set; } // 35, 36, 37...

        public decimal? Precio { get; set; } // opcional (si cambia)

        // Relación
        public int VarianteZapatoProveedorId { get; set; }
        public VarianteZapatoProveedor Variante { get; set; } = null!;
    }
}
