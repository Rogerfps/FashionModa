namespace FashionM.Models
{
    public class ClienteSemana
    {
        public int Id { get; set; }

        public int ClienteCedula { get; set; }

        public Clientes? Cliente { get; set; }

        public int Semana { get; set; }

        public int Año { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaVisita { get; set; }

        public string Observaciones { get; set; } = string.Empty;

        public string Usuario { get; set; } = string.Empty;
    }
}
