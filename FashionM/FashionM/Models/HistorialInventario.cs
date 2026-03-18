namespace FashionM.Models
{
    public class HistorialInventario
    {
        public int Id { get; set; }

        public string CodigoInventario { get; set; } = string.Empty;

        public string Accion { get; set; } = string.Empty; // CREAR, EDITAR, ELIMINAR

        public string Usuario { get; set; } = string.Empty;

        public DateTime Fecha { get; set; } = DateTime.Now;

        public string? Motivo { get; set; }

        public string? DatosAntes { get; set; }

        public string? DatosDespues { get; set; }

       // public Inventario? Inventario { get; set; } No deja hacer delete
    }
}
