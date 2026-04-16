namespace FashionM.Models.Provedor
{
    public class ZapatoProveedor
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; } = DateTime.Now;

        public string? ImagenUrl { get; set; }

        // Relaciones
        public int ProveedorCatalogoId { get; set; }
        public ProveedorCatalogo Proveedor { get; set; } = null!;

        public int EmpresaId { get; set; } // ya tienes esta tabla
        public Empresa Empresa { get; set; } = null!;

        public ICollection<VarianteZapatoProveedor> Variantes { get; set; } = new List<VarianteZapatoProveedor>();
    }
}

