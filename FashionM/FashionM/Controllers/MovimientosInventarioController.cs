using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Bodega")]
    public class MovimientosInventarioController : Controller
    {
        private readonly AppDbContext _context;

        public MovimientosInventarioController(AppDbContext context)
        {
            _context = context;
        }

        // ===============================
        // LISTA DE MOVIMIENTOS
        // ===============================
        public IActionResult Index(int page = 1)
        {
            int pageSize = 10;

            var query = _context.MovimientosInventario
                .Include(m => m.Inventario)
                .Include(m => m.Detalles)
                .OrderByDescending(m => m.Fecha);

            int totalItems = query.Count();

            var movimientos = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            return View(movimientos);
        }

        // ===============================
        // CREATE (GET)
        // ===============================
        public IActionResult Create()
        {
            ViewBag.Inventarios = _context.Inventarios
                .Select(i => new {
                    codigo = i.Codigo,
                    marca = i.Marca
                })
                .ToList();

            return View();
        }

        // ===============================
        // CREATE (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MovimientoInventario movimiento)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Inventarios = _context.Inventarios
                    .Select(i => new {
                        codigo = i.Codigo,
                        marca = i.Marca
                    })
                    .ToList();

                return View(movimiento);
            }

            if (!movimiento.Detalles.Any(d => d.Cantidad > 0))
            {
                ModelState.AddModelError("", "Debe ingresar al menos una cantidad.");

                ViewBag.Inventarios = _context.Inventarios
                    .Select(i => new {
                        codigo = i.Codigo,
                        marca = i.Marca
                    })
                    .ToList();

                return View(movimiento);
            }

            movimiento.Fecha = DateTime.UtcNow;

            _context.MovimientosInventario.Add(movimiento);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // DETALLE DEL MOVIMIENTO
        // ===============================
        public async Task<IActionResult> Details(int id)
        {
            var movimiento = await _context.MovimientosInventario
                .Include(m => m.Inventario)
                .Include(m => m.Detalles)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimiento == null)
                return NotFound();

            return View(movimiento);
        }

        // ===============================
        // ELIMINAR MOVIMIENTO
        // ===============================
        public async Task<IActionResult> Delete(int id)
        {
            var movimiento = await _context.MovimientosInventario
                .Include(m => m.Detalles)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimiento == null)
                return NotFound();

            return View(movimiento);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movimiento = await _context.MovimientosInventario
                .Include(m => m.Detalles)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movimiento != null)
            {
                _context.MovimientosInventario.Remove(movimiento);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Grafico()
        {
            var data = await _context.HistorialInventarios
                .ToListAsync();

            var entradas = data.Count(h => h.Accion == "CREAR");
            var salidas = data.Count(h => h.Accion == "ELIMINAR");

            var model = new
            {
                Entradas = entradas,
                Salidas = salidas
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult BuscarInventarios(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var productos = _context.Inventarios
                .Where(i => i.Codigo.Contains(term) || i.Marca.Contains(term))
                .Take(10)
                .Select(i => new
                {
                    codigo = i.Codigo,
                    marca = i.Marca
                })
                .ToList();

            return Json(productos);
        }
    }
}

