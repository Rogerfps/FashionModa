using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FashionM.Models
{
    public class TallaInventario
    {
        public int Id { get; set; }

        public string Color { get; set; } = string.Empty;

        public string? Detalle { get; set; }

        public string Numero { get; set; } = string.Empty;

        public int Cantidad { get; set; }

        [ValidateNever]
        public string InventarioCodigo { get; set; }

        [ValidateNever]
        public Inventario? Inventario { get; set; }
    }
}