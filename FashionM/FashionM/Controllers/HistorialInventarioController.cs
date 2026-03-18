using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FashionM.Data;

namespace FashionM.Controllers
{
    public class HistorialInventarioController : Controller
    {
        private readonly AppDbContext _context;

        public HistorialInventarioController(AppDbContext context)
        {
            _context = context;
        }

        // ===========================
        // HISTORIAL POR PRODUCTO
        // ===========================
        public async Task<IActionResult> PorProducto(string codigo)
        {
            if (string.IsNullOrEmpty(codigo))
                return NotFound();

            var historial = await _context.HistorialInventarios
                .Where(h => h.CodigoInventario == codigo)
                .OrderByDescending(h => h.Fecha)
                .ToListAsync();

            ViewBag.Codigo = codigo;

            return View("PorProducto", historial);
        }

        // ===========================
        // HISTORIAL GENERAL
        // ===========================
        public async Task<IActionResult> Index()
        {
            var historial = await _context.HistorialInventarios
                .OrderByDescending(h => h.Fecha)
                .Take(100) // limitar para performance
                .ToListAsync();

            return View(historial);
        }

        // ===========================
        // DETALLE (VER JSON)
        // ===========================
        public async Task<IActionResult> Detalle(int id)
        {
            var item = await _context.HistorialInventarios
                .FirstOrDefaultAsync(h => h.Id == id);

            if (item == null)
                return NotFound();

            return View(item);
        }
    }
}

