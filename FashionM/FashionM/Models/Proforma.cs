using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Proforma
    {
        [Key]
        public int Id { get; set; }
        public int Numero { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }

        public int ClienteCedula { get; set; }

        public Clientes? Cliente { get; set; }

        public decimal Total { get; set; }

        public string Observaciones { get; set; } = string.Empty;

        public string FacturadoPor { get; set; } = string.Empty;
        public string AgenteVenta { get; set; } = string.Empty;

        public int NumeroCajas { get; set; }

        public string? Detalle { get; set; }

        public ICollection<ProformaDetalle> Detalles { get; set; } = new List<ProformaDetalle>();
    }
}

