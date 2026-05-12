using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Venta
    {
        [Key]
        public int Id { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        // Relación con documento origen
        public string TipoDocumento { get; set; } = string.Empty;
        // PROFORMA
        // FACTURA_ELECTRONICA

        public int DocumentoId { get; set; }

        // Cliente
        public int ClienteCedula { get; set; }

        public Clientes? Cliente { get; set; }

        // Empresa
        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }

        // Datos venta
        public decimal Total { get; set; }

        public int NumeroCajas { get; set; }

        public string FacturadoPor { get; set; } = string.Empty;

        public string AgenteVenta { get; set; } = string.Empty;

        // Estados
        public string Estado { get; set; } = "ACTIVA";

        // Métricas
        public int Semana { get; set; }

        public int Mes { get; set; }

        public int Año { get; set; }

        // Navegación
        public ICollection<VentaDetalle> Detalles { get; set; }
            = new List<VentaDetalle>();
    }
}

