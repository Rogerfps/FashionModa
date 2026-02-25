using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    public class InventariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public InventariosController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Inventarios
        public IActionResult Index()
        {
            var inventarios = _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .ToList();

            return View(inventarios);
        }

        // GET: Inventarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Inventario inventario,
            List<IFormFile> imagenes)
        {
            if (inventario.Tallas == null || !inventario.Tallas.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos una talla");
            }

            if (!ModelState.IsValid)
            {
                return View(inventario);
            }

            foreach (var talla in inventario.Tallas)
            {
                talla.InventarioCodigo = inventario.Codigo;
                talla.Inventario = inventario;
            }

            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int codigo)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == codigo);

            if (inventario == null)
                return NotFound();

            // 🗑️ borrar imágenes físicas
            foreach (var foto in inventario.Fotos)
            {
                var ruta = Path.Combine(
                    _environment.WebRootPath,
                    foto.Ruta.Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);
            }

            _context.Inventarios.Remove(inventario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}