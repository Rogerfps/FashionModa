using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // ===============================
        // LISTAR
        // ===============================
        public async Task<IActionResult> Index(string buscar, bool? estado, int page = 1)
        {
            int pageSize = 25;
            var clientes = _context.Clientes.AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                clientes = clientes.Where(c =>
                    c.Nombre.Contains(buscar) ||
                    c.Apellidos.Contains(buscar) ||
                    c.Cedula.ToString().Contains(buscar) ||
                    (c.Telefonos != null && c.Telefonos.Contains(buscar)) ||
                    c.Codigo.Contains(buscar)
                );
            }

            if (estado.HasValue)
            {
                clientes = clientes.Where(c => c.Estado == estado.Value);
            }

            int totalRegistros = await clientes.CountAsync();

            var lista = await clientes
                .OrderBy(c => c.Cedula)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.PaginaActual = page;

            return View(lista);
        }

        // ===============================
        // CREATE (GET)
        // ===============================
        public IActionResult Create()
        {
            CargarTiposIdentificacion();
            return View();
        }

        // ===============================
        // CREATE (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Clientes cliente)
        {
            CargarTiposIdentificacion();

            bool existeCedula = await _context.Clientes
                .AnyAsync(c => c.Cedula == cliente.Cedula);

            if (existeCedula)
            {
                ModelState.AddModelError("Cedula", "La cédula ya existe.");
            }

            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // DETAILS
        // ===============================
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
                return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        // ===============================
        // EDIT (GET)
        // ===============================
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0)
                return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == id);

            if (cliente == null)
                return NotFound();

            CargarTiposIdentificacion();
            return View(cliente);
        }

        // ===============================
        // EDIT (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Clientes cliente)
        {
            CargarTiposIdentificacion();

            if (!ModelState.IsValid)
                return View(cliente);

            try
            {
                _context.Update(cliente);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clientes.Any(c => c.Cedula == cliente.Cedula))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // DELETE (GET)
        // ===============================
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0)
                return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        // ===============================
        // DELETE (POST)
        // ===============================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Cedula)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == Cedula);

            if (cliente == null)
                return NotFound();

            _context.Clientes.Remove(cliente);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // MÉTODO PRIVADO
        // ===============================
        private void CargarTiposIdentificacion()
        {
            ViewBag.TiposIdentificacion = new List<SelectListItem>
        {
            new SelectListItem { Value = "Cedula Fisica", Text = "Cédula Física" },
            new SelectListItem { Value = "Cedula Juridica", Text = "Cédula Jurídica" },
            new SelectListItem { Value = "Dimex", Text = "DIMEX" },
            new SelectListItem { Value = "Nite", Text = "NITE" },
            new SelectListItem { Value = "Extranjero", Text = "Extranjero" }
        };
        }
    }
}

