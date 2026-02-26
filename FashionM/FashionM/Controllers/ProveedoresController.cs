using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly AppDbContext _context;

        public ProveedoresController(AppDbContext context)
        {
            _context = context;
        }

        // LISTAR
        public async Task<IActionResult> Index(string buscar, bool? estado, int page = 1)
        {
            int pageSize = 5;

            var proveedores = _context.Proveedores.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                proveedores = proveedores.Where(p =>
                    p.Nombre.Contains(buscar) ||
                    p.Apellidos.Contains(buscar) ||
                    p.Cedula.ToString().Contains(buscar) ||
                    p.Telefono.ToString().Contains(buscar)
                );
            }

            if (estado.HasValue)
            {
                proveedores = proveedores.Where(p => p.Estado == estado.Value);
            }

            int totalRegistros = await proveedores.CountAsync();

            var lista = await proveedores
                .OrderBy(p => p.Cedula)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.PaginaActual = page;

            return View(lista);
        }

        // CREATE
        public IActionResult Create()
        {
            ViewBag.TiposIdentificacion = new List<SelectListItem>
        {
            new SelectListItem { Value = "Cedula Fisica", Text = "Cédula Física" },
            new SelectListItem { Value = "Cedula Juridica", Text = "Cédula Jurídica" },
            new SelectListItem { Value = "Dimex", Text = "DIMEX" },
            new SelectListItem { Value = "Nite", Text = "NITE" },
            new SelectListItem { Value = "Extranjero", Text = "Extranjero" }
        };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedores proveedor)
        {
            ViewBag.TiposIdentificacion = new List<SelectListItem>
        {
            new SelectListItem { Value = "Cedula Fisica", Text = "Cédula Física" },
            new SelectListItem { Value = "Cedula Juridica", Text = "Cédula Jurídica" },
            new SelectListItem { Value = "Dimex", Text = "DIMEX" },
            new SelectListItem { Value = "Nite", Text = "NITE" },
            new SelectListItem { Value = "Extranjero", Text = "Extranjero" }
        };

            if (!ModelState.IsValid)
                return View(proveedor);

            _context.Add(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        // EDIT
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();

            ViewBag.TiposIdentificacion = new List<SelectListItem>
        {
            new SelectListItem { Value = "Cedula Fisica", Text = "Cédula Física" },
            new SelectListItem { Value = "Cedula Juridica", Text = "Cédula Jurídica" },
            new SelectListItem { Value = "Dimex", Text = "DIMEX" },
            new SelectListItem { Value = "Nite", Text = "NITE" },
            new SelectListItem { Value = "Extranjero", Text = "Extranjero" }
        };

            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedores proveedor)
        {
            if (!ModelState.IsValid)
                return View(proveedor);

            _context.Update(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null) return NotFound();
            return View(proveedor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}

