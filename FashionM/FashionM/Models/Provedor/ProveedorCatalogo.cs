namespace FashionM.Models.Provedor
{
    public class ProveedorCatalogo
    {
        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Codigo { get; set; } = string.Empty; 

        public string Cedula { get; set; } = string.Empty;

        public string Telefonos { get; set; } = string.Empty; 

        public string Direccion { get; set; } = string.Empty;

        public string Correo { get; set; } = string.Empty;

        public string ActividadEconomica { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        // Relaciones
        public ICollection<ZapatoProveedor> Zapatos { get; set; } = new List<ZapatoProveedor>();
    }
}
