using Microsoft.AspNetCore.Identity;

namespace FashionM.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Nombre { get; set; }

    }
}
