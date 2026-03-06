using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FashionM.Models
{
    public class TallaInventario
    {
        public int Id { get; set; }

        public string Numero { get; set; }

        public int Cantidad { get; set; }

        // FK
        public string? InventarioCodigo { get; set; }

        [NotMapped]
        public Inventario? Inventario { get; set; }
    }
}