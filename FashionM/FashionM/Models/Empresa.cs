using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class Empresa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string CedulaJuridica { get; set; } = string.Empty;

        public string Telefono { get; set; } = string.Empty;

        public string Direccion { get; set; } = string.Empty;

        public string CuentaBAC { get; set; } = string.Empty;

        public string CuentaBCR { get; set; } = string.Empty;

        public string CuentaBN { get; set; } = string.Empty;

        public string SimpeMovil { get; set; } = string.Empty;

        public ICollection<Proforma> Proformas { get; set; } = new List<Proforma>();
    }
}
