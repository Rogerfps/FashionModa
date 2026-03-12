using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class ProveedoresController : Controller
    {
        private readonly AppDbContext _context;

        public ProveedoresController(AppDbContext context)
        {
            _context = context;
        }

        // =========================
        // LISTAR
        // =========================
        public async Task<IActionResult> Index(string buscar, bool? estado, string empresa, int page = 1)
        {
            int pageSize = 25;

            var proveedores = _context.Proveedores.AsQueryable();

            // 🔍 BÚSQUEDA GENERAL
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                proveedores = proveedores.Where(p =>
                    p.Nombre.Contains(buscar) ||
                    p.Apellidos.Contains(buscar) ||
                    p.Cedula.ToString().Contains(buscar) ||
                    p.Telefono.ToString().Contains(buscar)
                );
            }

            // ✅ FILTRO ESTADO
            if (estado.HasValue)
            {
                proveedores = proveedores.Where(p => p.Estado == estado.Value);
            }

            // 🏢 FILTRO POR EMPRESA 
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                var e = empresa.Trim();

                proveedores = proveedores.Where(p =>
                    p.Empresa != null &&
                    (
                        p.Empresa == e ||
                        EF.Functions.Like(p.Empresa, $"{e}|%") ||
                        EF.Functions.Like(p.Empresa, $"%|{e}") ||
                        EF.Functions.Like(p.Empresa, $"%|{e}|%")
                    )
                );
            }


                ViewBag.Empresas = _context.Proveedores
                    .Where(p => !string.IsNullOrWhiteSpace(p.Empresa))
                    .AsEnumerable()                 
                    .SelectMany(p => p.Empresa.Split('|'))
                    .Select(e => e.Trim())
                    .Distinct()
                    .OrderBy(e => e)
                    .ToList();


            ViewBag.EmpresaSeleccionada = empresa;

            // 📊 PAGINACIÓN
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

        // =========================
        // CREATE
        // =========================
        public IActionResult Create()
        {
            CargarTiposIdentificacion();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proveedor proveedor)
        {
            CargarTiposIdentificacion();

            if (!ModelState.IsValid)
                return View(proveedor);

            // 🔴 VALIDAR CÉDULA DUPLICADA
            bool existe = await _context.Proveedores
                .AnyAsync(p => p.Cedula == proveedor.Cedula);

            if (existe)
            {
                ModelState.AddModelError(
                    "Cedula",
                    "Ya existe un proveedor con esta cédula."
                );

                return View(proveedor);
            }

            _context.Proveedores.Add(proveedor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DETAILS (con relaciones)
        // =========================
        public async Task<IActionResult> Details(int id)
        {
            var proveedor = await _context.Proveedores
            .Include(p => p.Zapatos)
            .ThenInclude(z => z.Imagenes)
            .FirstOrDefaultAsync(p => p.Cedula == id);

            if (proveedor == null)
                return NotFound();

            return View(proveedor);
        }


        // =========================
        // EDIT
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor == null)
                return NotFound();

            CargarTiposIdentificacion();
            return View(proveedor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedor proveedor)
        {
            if (!ModelState.IsValid)
            {
                CargarTiposIdentificacion();
                return View(proveedor);
            }

            // 🔴 Traer el proveedor original
            var proveedorDB = await _context.Proveedores
                .FirstOrDefaultAsync(p => p.Cedula == proveedor.Cedula);

            if (proveedorDB == null)
                return NotFound();

            // 🔴 Actualizar SOLO campos editables
            proveedorDB.Nombre = proveedor.Nombre;
            proveedorDB.Apellidos = proveedor.Apellidos;
            proveedorDB.IDTipo = proveedor.IDTipo;
            proveedorDB.Correo = proveedor.Correo;
            proveedorDB.Telefono = proveedor.Telefono;
            proveedorDB.Comercio = proveedor.Comercio;
            proveedorDB.Direccion = proveedor.Direccion;
            proveedorDB.Actividad = proveedor.Actividad;
            proveedorDB.Empresa = proveedor.Empresa;
            proveedorDB.Estado = proveedor.Estado;


            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // DELETE
        // =========================
        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _context.Proveedores
                .Include(p => p.Zapatos)
                .FirstOrDefaultAsync(p => p.Cedula == id);

            if (proveedor == null)
                return NotFound();


            return View(proveedor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores
        .Include(p => p.Zapatos)
        .FirstOrDefaultAsync(p => p.Cedula == id);

            if (proveedor == null)
                return NotFound();

            _context.Proveedores.Remove(proveedor);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // MÉTODO AUXILIAR
        // =========================
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

