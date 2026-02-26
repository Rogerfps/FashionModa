using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class Inventario
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Codigo { get; set; }

        public string Marca { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Detalle { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string CodigoCabys { get; set; } = string.Empty;

        public decimal PrecioCosto { get; set; }
        public decimal PrecioVenta { get; set; }

        public List<TallaInventario> Tallas { get; set; } = new();
        public List<Foto> Fotos { get; set; } = new();

        [NotMapped]
        public int StockTotal => Tallas.Sum(t => t.Cantidad);
    }
}
