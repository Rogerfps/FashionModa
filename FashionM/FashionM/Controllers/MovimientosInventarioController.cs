using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
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
                .OrderBy(i => i.Marca)
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
                ViewBag.Inventarios = _context.Inventarios.ToList();
                return View(movimiento);
            }

            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .FirstOrDefaultAsync(i => i.Codigo == movimiento.InventarioCodigo);

            if (inventario == null)
                return NotFound();

            foreach (var detalle in movimiento.Detalles)
            {
                var talla = inventario.Tallas
                    .FirstOrDefault(t => t.Numero == detalle.Numero);

                if (talla == null)
                {
                    talla = new TallaInventario
                    {
                        Numero = detalle.Numero,
                        Cantidad = 0,
                        InventarioCodigo = inventario.Codigo
                    };

                    inventario.Tallas.Add(talla);
                }

                if (movimiento.Tipo == "Entrada")
                {
                    talla.Cantidad += detalle.Cantidad;
                }
                else if (movimiento.Tipo == "Salida")
                {
                    if (talla.Cantidad < detalle.Cantidad)
                    {
                        ModelState.AddModelError("", $"Stock insuficiente en talla {detalle.Numero}");
                        ViewBag.Inventarios = _context.Inventarios.ToList();
                        return View(movimiento);
                    }

                    talla.Cantidad -= detalle.Cantidad;
                }
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
    }
}

