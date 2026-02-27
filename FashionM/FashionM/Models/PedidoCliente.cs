using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class PedidoCliente
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteCedula { get; set; }

        [ForeignKey(nameof(ClienteCedula))]
        public Clientes Cliente { get; set; } = null!;

        public DateTime FechaPedido { get; set; } = DateTime.UtcNow;
        public DateTime FechaEntrega { get; set; } = DateTime.UtcNow.AddDays(60);

        public string? Observaciones { get; set; }

        [Required]
        public string Vendedor { get; set; } = string.Empty;

        public bool FirmaCredito { get; set; }
        public bool FirmaBodega { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public ICollection<PedidoClienteDetalle> Detalles { get; set; } = new List<PedidoClienteDetalle>();
    }
}
