using FashionM.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace FashionM.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}