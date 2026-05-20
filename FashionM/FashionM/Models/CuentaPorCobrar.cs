using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class CuentaPorCobrar
    {
        [Key]
        public int Id { get; set; }

        // ========================================
        // RELACIÓN VENTA
        // ========================================

        [Required]
        public int VentaId { get; set; }

        public Venta? Venta { get; set; }

        // ========================================
        // RELACIÓN CLIENTE
        // ========================================

        [Required]
        public int ClienteCedula { get; set; }

        [ForeignKey(nameof(ClienteCedula))]
        public Clientes? Cliente { get; set; }

        // ========================================
        // DATOS
        // ========================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoOriginal { get; set; }

        public DateTime Fecha { get; set; }

        public DateTime? FechaVencimiento { get; set; }
        public int DiasCredito { get; set; }

        [StringLength(50)]
        public string Estado { get; set; }
            = "PENDIENTE";

        [StringLength(500)]
        public string Observaciones { get; set; }
            = string.Empty;

        // ========================================
        // PAGOS
        // ========================================

        public ICollection<CuentaPorCobrarPago> Pagos
        { get; set; }
            = new List<CuentaPorCobrarPago>();
    }
}
    

