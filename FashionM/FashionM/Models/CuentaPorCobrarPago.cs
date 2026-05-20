using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class CuentaPorCobrarPago
    {
        [Key]
        public int Id { get; set; }

        // ========================================
        // RELACIÓN CUENTA
        // ========================================

        [Required]
        public int CuentaPorCobrarId { get; set; }

        public CuentaPorCobrar? CuentaPorCobrar
        { get; set; }

        // ========================================
        // DATOS PAGO
        // ========================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(100)]
        public string MetodoPago { get; set; }
            = string.Empty;

        [StringLength(500)]
        public string? Observacion { get; set; }
            = string.Empty;

        // ========================================
        // FOTO RECIBO
        // ========================================

        [StringLength(500)]
        public string? FotoRecibo { get; set; }
    }
}
    

