using System.ComponentModel.DataAnnotations;
using System.Drawing;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;


namespace FashionM.Models
{
    public class Codigo
    {
        [Key]
        public int IdCodigo { get; set; }

        [Required]
        public string NombreCodigo { get; set; } = string.Empty;

        
        public int ProveedorCedula { get; set; }
        public Proveedor Proveedor { get; set; }

        
        public ICollection<Color> Colores { get; set; } = new List<Color>();
    }
}

