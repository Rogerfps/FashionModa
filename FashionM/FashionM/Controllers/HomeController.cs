using FashionM.Models;
using Microsoft.AspNetCore.Mvc;
using FashionM.Data;

namespace FashionM.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var empresa = HttpContext.Session.GetInt32("EmpresaId");

            if (empresa == null)
            {
                return RedirectToAction(nameof(SeleccionarEmpresa));
            }

            return View();
        }

        public IActionResult SeleccionarEmpresa()
        {
            return View();
        }

        public IActionResult CambiarEmpresa(int id, string nombre)
        {
            var empresa = _context.Empresas
                .FirstOrDefault(e => e.Nombre == nombre);

            if (empresa == null)
            {
                return RedirectToAction(nameof(SeleccionarEmpresa));
            }

            HttpContext.Session.SetInt32("EmpresaId", empresa.Id);
            HttpContext.Session.SetString("EmpresaNombre", empresa.Nombre);

            return RedirectToAction(nameof(Index));
        }
    }
}