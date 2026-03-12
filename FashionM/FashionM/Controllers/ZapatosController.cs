using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
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
            var zapato = new Zapato
            {
                ProveedorCedula = proveedorCedula
            };

            return View(zapato);
        }

        // =========================
        // CREATE (POST)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    Zapato zapato,
    List<IFormFile> imagenes
)
        {
            if (!ModelState.IsValid)
            {
                return View(zapato);
            }

            // 1️⃣ Guardar zapato primero
            _context.Zapatos.Add(zapato);
            await _context.SaveChangesAsync(); // ← AQUÍ ya tenemos zapato.Id

            // 2️⃣ Guardar imágenes
            if (imagenes != null && imagenes.Count > 0)
            {
                foreach (var file in imagenes)
                {
                    if (file.Length == 0) continue;

                    // Nombre único
                    var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                    var rutaCarpeta = Path.Combine("wwwroot", "imagenes", "zapatos");

                    if (!Directory.Exists(rutaCarpeta))
                        Directory.CreateDirectory(rutaCarpeta);

                    var rutaFisica = Path.Combine(rutaCarpeta, fileName);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // 3️⃣ Guardar en BD
                    var imagen = new ImagenZapato
                    {
                        ZapatoId = zapato.Id, // 🔴 CLAVE
                        Url = $"/imagenes/zapatos/{fileName}"
                    };

                    _context.Add(imagen);
                }

                await _context.SaveChangesAsync();
            }

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