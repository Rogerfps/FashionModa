using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FashionM.Models.Provedor
{
    public class ZapatoProveedor
    {
        public int Id { get; set; }

        public string Codigo { get; set; } = string.Empty;

        public DateTime FechaIngreso { get; set; } = DateTime.UtcNow;

        public string Empresa { get; set; } = string.Empty;

        // 🖼️ Imagen principal
        public string? ImagenUrl { get; set; }

        // 💰 Precios base
        public decimal? PrecioVenta { get; set; }

        public decimal? PrecioCosto { get; set; }

        public decimal? PrecioColombia { get; set; }

        // 🔗 Relación
        public int ProveedorCatalogoId { get; set; }

        [ValidateNever]
        public ProveedorCatalogo Proveedor { get; set; } = null!;

        // 🔥 Listas
        public ICollection<ColorZapato> Colores { get; set; } = new List<ColorZapato>();
        public ICollection<SuelaZapato> Suelas { get; set; } = new List<SuelaZapato>();
        public ICollection<DetalleZapato> Detalles { get; set; } = new List<DetalleZapato>();
        public ICollection<TallaZapato> Tallas { get; set; } = new List<TallaZapato>();
    }
}


