using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FashionM.Models.Provedor
{
    public class DetalleZapato
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public int ZapatoProveedorId { get; set; }

        [ValidateNever]
        public ZapatoProveedor Zapato { get; set; } = null!;
    }
}