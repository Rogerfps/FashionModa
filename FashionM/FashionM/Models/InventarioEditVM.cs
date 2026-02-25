using System.ComponentModel.DataAnnotations;

namespace FashionM.Models
{
    public class InventarioEditVM
    {
        public int Codigo { get; set; }

        public string Marca { get; set; }
        public string Color { get; set; }
        public string Detalle { get; set; }
        public string SKU { get; set; }

        public decimal PrecioCosto { get; set; }
        public decimal PrecioVenta { get; set; }

        //  FOTOS EXISTENTES
        public List<Foto> Fotos { get; set; } = new();

        //  NUEVAS FOTOS
        public List<IFormFile> NuevasFotos { get; set; } = new();
        public List<TallaInventario> Tallas { get; set; } = new();
    }

}

