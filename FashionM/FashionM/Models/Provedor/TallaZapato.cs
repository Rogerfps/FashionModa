using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace FashionM.Models.Provedor
{
    public class TallaZapato
    {
        public int Id { get; set; }

        public int Numero { get; set; }

        public decimal? Precio { get; set; }
        public decimal? PrecioColombia { get; set; }

        public int ZapatoProveedorId { get; set; }

        [ValidateNever]
        public ZapatoProveedor Zapato { get; set; } = null!;
    }
}
