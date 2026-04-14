using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class ProveedoresController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProveedoresController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
        // EDIT GET
        // =========================
        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _context.Proveedores
                .Include(p => p.Zapatos)
                .FirstOrDefaultAsync(p => p.Cedula == id);

            if (proveedor == null)
                return NotFound();

            CargarTiposIdentificacion();

            return View(proveedor);
        }


        // =========================
        // EDIT POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Proveedor model)
        {
            if (!ModelState.IsValid)
            {
                CargarTiposIdentificacion();
                return View(model);
            }

            // 🔴 Traer proveedor real con zapatos
            var proveedorDB = await _context.Proveedores
                .Include(p => p.Zapatos)
                .FirstOrDefaultAsync(p => p.Cedula == model.Cedula);

            if (proveedorDB == null)
                return NotFound();

            // =========================
            // 🔵 ACTUALIZAR PROVEEDOR
            // =========================
            proveedorDB.Nombre = model.Nombre;
            proveedorDB.Apellidos = model.Apellidos;
            proveedorDB.IDTipo = model.IDTipo;
            proveedorDB.Correo = model.Correo;
            proveedorDB.Telefono = model.Telefono;
            proveedorDB.Comercio = model.Comercio;
            proveedorDB.Direccion = model.Direccion;
            proveedorDB.Actividad = model.Actividad;
            proveedorDB.Empresa = model.Empresa;
            proveedorDB.Estado = model.Estado;

            // =========================
            // 🟡 MANEJO DE ZAPATOS
            // =========================

            var zapatosForm = model.Zapatos ?? new List<Zapato>();

            // IDs enviados
            var idsEnviados = zapatosForm
                .Where(z => z.Id != 0)
                .Select(z => z.Id)
                .ToList();

            // 🔴 ELIMINAR los que quitaste en la vista
            var eliminar = proveedorDB.Zapatos
                .Where(z => !idsEnviados.Contains(z.Id))
                .ToList();

            _context.Zapatos.RemoveRange(eliminar);

            // 🔄 ACTUALIZAR o CREAR
            foreach (var z in zapatosForm)
            {
                if (z.Id != 0)
                {
                    var zapatoDB = proveedorDB.Zapatos
                        .FirstOrDefault(x => x.Id == z.Id);

                    if (zapatoDB != null)
                    {
                        zapatoDB.Codigo = z.Codigo;
                        zapatoDB.Color = z.Color;
                        zapatoDB.Suela = z.Suela;
                        zapatoDB.Detalle = z.Detalle;
                        zapatoDB.Numero = z.Numero;
                        zapatoDB.Cantidad = z.Cantidad;
                        zapatoDB.PrecioVenta = z.PrecioVenta;
                        zapatoDB.PrecioCosto = z.PrecioCosto;
                    }
                }
                else
                {
                    proveedorDB.Zapatos.Add(new Zapato
                    {
                        Codigo = z.Codigo,
                        Color = z.Color,
                        Suela = z.Suela,
                        Detalle = z.Detalle,
                        Numero = z.Numero,
                        Cantidad = z.Cantidad,
                        PrecioVenta = z.PrecioVenta,
                        PrecioCosto = z.PrecioCosto,
                        ProveedorCedula = proveedorDB.Cedula
                    });
                }
            }

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

