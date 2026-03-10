using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class Foto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Ruta { get; set; } = string.Empty;

        // FK
        [ValidateNever]
        public string? InventarioCodigo { get; set; } = string.Empty;

        [ValidateNever]
        public Inventario? Inventario { get; set; }
    }
}
