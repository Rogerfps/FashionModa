using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    public class ZapatosController : Controller
    {
        private readonly AppDbContext _context;

        public ZapatosController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // LISTAR POR PROVEEDOR
        // =========================
        public async Task<IActionResult> Index(int proveedorCedula)
        {
            var zapatos = await _context.Zapatos
                .Where(z => z.ProveedorCedula == proveedorCedula)
                .ToListAsync();

            ViewBag.ProveedorCedula = proveedorCedula;
            return View(zapatos);
        }

        // =========================
        // DETAILS
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var zapato = await _context.Zapatos
                .FirstOrDefaultAsync(z => z.Id == id);

            if (zapato == null)
                return NotFound();

            return View(zapato);
        }

        // =========================
        // CREATE (GET)
        // =========================
        public IActionResult Create(int proveedorCedula)
        {
            return View(new Zapato
            {
                ProveedorCedula = proveedorCedula
            });
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Zapato zapato)
        {
            Console.WriteLine("ENTRÓ AL POST CREATE");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("MODELSTATE INVALIDO");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                return View(zapato);
            }

            _context.Zapatos.Add(zapato);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Proveedores",
                new { id = zapato.ProveedorCedula }
            );
        }

        // =========================
        // EDIT (GET)
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var zapato = await _context.Zapatos.FindAsync(id);

            if (zapato == null)
                return NotFound();

            return View(zapato);
        }

        // =========================
        // EDIT (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Zapato zapato)
        {
            if (!ModelState.IsValid)
                return View(zapato);

            _context.Update(zapato);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Proveedores",
                new { id = zapato.ProveedorCedula }
            );
        }

        // =========================
        // DELETE (GET)
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var zapato = await _context.Zapatos
                .FirstOrDefaultAsync(z => z.Id == id);

            if (zapato == null)
                return NotFound();

            return View(zapato);
        }

        // =========================
        // DELETE (POST)
        // =========================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var zapato = await _context.Zapatos.FindAsync(id);

            if (zapato == null)
                return NotFound();

            int proveedorCedula = zapato.ProveedorCedula;

            _context.Zapatos.Remove(zapato);
            await _context.SaveChangesAsync();

            return RedirectToAction(
                "Details",
                "Proveedores",
                new { id = proveedorCedula }
            );
        }
    }
}