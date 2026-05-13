using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class NotaCredito
    {
        [Key]
        public int Id { get; set; }

        public int VentaId { get; set; }

        public Venta? Venta { get; set; }

        public DateTime Fecha { get; set; }
            = DateTime.UtcNow;

        public string Motivo { get; set; }
            = string.Empty;

        public string TipoDocumento { get; set; }
            = string.Empty;

        public string Estado { get; set; }
            = "ACTIVA";

        public int ClienteCedula { get; set; }

        public Clientes? Cliente { get; set; }

        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }

        // TOTALES
        public decimal SubTotal { get; set; }

        public decimal DescuentoGlobal { get; set; }

        public decimal TotalDevuelto { get; set; }

        // MÉTRICAS
        public int Semana { get; set; }

        public int Mes { get; set; }

        public int Año { get; set; }

        // AGENTE
        public string AgenteVenta { get; set; }
            = string.Empty;

        // DETALLES
        public ICollection<NotaCreditoDetalle> Detalles
        { get; set; }
            = new List<NotaCreditoDetalle>();
    }
}

