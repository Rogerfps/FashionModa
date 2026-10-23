using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FashionM.Models
{
    public class ClienteCreditoEmpresa
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ClienteCedula { get; set; }

        [ForeignKey(nameof(ClienteCedula))]
        public Clientes Cliente { get; set; } = null!;

        [Required]
        public int EmpresaId { get; set; }

        [ForeignKey(nameof(EmpresaId))]
        public Empresa Empresa { get; set; } = null!;

        // true = crédito abierto, false = crédito cerrado
        public bool CreditoAbierto { get; set; } = true;

        public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;
    }
}
