using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FashionM.Models
{
    public class TallaInventario
    {
        public int Id { get; set; }

        public int Numero { get; set; }

        public int Cantidad { get; set; }

        // FK
        public int InventarioCodigo { get; set; }

        // ✅ NO MAPEADA (CLAVE)
        [NotMapped]
        public Inventario? Inventario { get; set; }
    }
}

