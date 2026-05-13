using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class NotaCreditoDetalle
    {
        [Key]
        public int Id { get; set; }

        public int NotaCreditoId { get; set; }

        public NotaCredito? NotaCredito { get; set; }

        public int? VentaDetalleId { get; set; }

        public VentaDetalle? VentaDetalle { get; set; }

        // PRODUCTO
        public string InventarioCodigo { get; set; }
            = string.Empty;

        public string Color { get; set; }
            = string.Empty;

        public string Talla { get; set; }
            = string.Empty;

        // CANTIDADES
        public int CantidadOriginal { get; set; }

        public int CantidadDevuelta { get; set; }

        // PRECIOS
        public decimal PrecioOriginal { get; set; }

        public decimal PrecioCorregido { get; set; }

        // DESCUENTOS
        public decimal DescuentoLinea { get; set; }

        // ELIMINACIÓN
        public bool Eliminado { get; set; }

        // OBSERVACIONES
        public string Observaciones { get; set; }
            = string.Empty;

        // TOTAL
        public decimal SubTotal { get; set; }
    }
}

