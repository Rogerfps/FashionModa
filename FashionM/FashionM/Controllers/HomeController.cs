using FashionM.Models;
using Microsoft.AspNetCore.Mvc;

namespace FashionM.Controllers
{
    public class HomeController : Controller
    {
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
            HttpContext.Session.SetInt32("EmpresaId", id);
            HttpContext.Session.SetString("EmpresaNombre", nombre);

            return RedirectToAction("Index");
        }
    }
}