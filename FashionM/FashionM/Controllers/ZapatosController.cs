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

        private readonly IWebHostEnvironment _environment;

        public ZapatosController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            ViewBag.ProveedorCedula = proveedorCedula;

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
        public async Task<IActionResult> Create(List<Zapato> modelos, List<IFormFile> imagenes)
        {
            if (modelos == null || !modelos.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos una talla");

                ViewBag.ProveedorCedula = modelos?.FirstOrDefault()?.ProveedorCedula;

                return View(new Zapato
                {
                    ProveedorCedula = modelos?.FirstOrDefault()?.ProveedorCedula ?? 0
                });
            }

            var proveedorCedula = modelos.First().ProveedorCedula;

            var existeProveedor = await _context.Proveedores
                .AnyAsync(p => p.Cedula == proveedorCedula);

            if (!existeProveedor)
            {
                ModelState.AddModelError("", "El proveedor no existe");

                ViewBag.ProveedorCedula = proveedorCedula;

                return View(new Zapato
                {
                    ProveedorCedula = proveedorCedula
                });
            }

            // 🔥 1. Guardar zapatos
            foreach (var zapato in modelos)
            {
                _context.Zapatos.Add(zapato);
            }

            await _context.SaveChangesAsync(); // 🔴 NECESARIO para obtener IDs

            // 🔥 2. Guardar imágenes
            if (imagenes != null && imagenes.Any())
            {
                var folder = Path.Combine(_environment.WebRootPath, "imagenes", "zapatos");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (var img in imagenes)
                {
                    if (img.Length == 0)
                        continue;

                    var nombre = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                    var rutaFisica = Path.Combine(folder, nombre);

                    using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await img.CopyToAsync(stream);
                    }

                    // 🔴 IMPORTANTE: asociar imágenes al PRIMER zapato (modelo base)
                    _context.ImagenesZapato.Add(new ImagenZapato
                    {
                        ZapatoId = modelos.First().Id,
                        Url = "/imagenes/zapatos/" + nombre
                    });
                }

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                "Details",
                "Proveedores",
                new { id = proveedorCedula }
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