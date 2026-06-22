using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using FashionM.Services;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria,Bodega")]
    public class InventariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public InventariosController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // INDEX
        public IActionResult Index(string search, string empresa, int page = 1)
        {
            int pageSize = 25;

            // Solo usar la empresa de la sesión la primera vez que entra
            if (!Request.Query.ContainsKey("empresa"))
            {
                empresa = HttpContext.Session.GetString("EmpresaNombre");

                if (string.IsNullOrWhiteSpace(empresa))
                {
                    return RedirectToAction("SeleccionarEmpresa", "Home");
                }
            }

            var query = _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Codigo.Contains(search) ||
                    i.Marca.Contains(search) ||
                    i.SKU.Contains(search) ||
                    i.Tallas.Any(t => t.Color.Contains(search)) ||
                    i.Tallas.Any(t => t.Detalle.Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(empresa))
            {
                query = query.Where(i => i.Empresa == empresa);
            }

            int totalItems = query.Count();

            var inventarios = query
                .OrderBy(i => i.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;
            ViewBag.Empresa = empresa;

            ViewBag.Empresas = _context.Inventarios
                .Select(i => i.Empresa)
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            return View(inventarios);
        }


        [HttpGet]
        public IActionResult ExportarExcel(string? search, string? empresa)
        {
            // Si no viene empresa en la URL, usar la de la sesión
            if (!Request.Query.ContainsKey("empresa"))
            {
                empresa = HttpContext.Session.GetString("EmpresaNombre");

                if (string.IsNullOrWhiteSpace(empresa))
                {
                    return RedirectToAction("SeleccionarEmpresa", "Home");
                }
            }

            var query = _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .AsQueryable();

            // Buscar
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Codigo.Contains(search) ||
                    i.Marca.Contains(search) ||
                    i.SKU.Contains(search) ||
                    i.Tallas.Any(t => t.Color.Contains(search)) ||
                    i.Tallas.Any(t => t.Detalle.Contains(search))
                );
            }

            // Empresa
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                query = query.Where(i => i.Empresa == empresa);
            }

            var inventarios = query
                .OrderBy(i => i.Codigo)
                .ToList();

            var excel = new ExcelInventarioService(_environment);

            byte[] archivo = excel.Generar(inventarios);

            return File(
                archivo,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Inventario_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }

        // CREATE GET
        public IActionResult Create()
        {
            ViewBag.Proveedores = _context.ProveedoresCatalogo
                .Where(p => p.Activo)
                .ToList();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Inventario inventario, List<IFormFile> imagenes)
        {
            inventario.Fotos ??= new List<Foto>();
            inventario.Tallas ??= new List<TallaInventario>();

            if (!inventario.Tallas.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos una talla");
            }

            if (_context.Inventarios.Any(i => i.Codigo == inventario.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un inventario con este código");
            }

            if (!ModelState.IsValid)
                return View(inventario);

            foreach (var talla in inventario.Tallas)
            {
                talla.InventarioCodigo = inventario.Codigo;
                talla.Color ??= "";
                talla.Detalle ??= "";
            }

            if (imagenes != null && imagenes.Count > 0)
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "inventarios");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (var img in imagenes)
                {
                    if (img.Length == 0)
                        continue;

                    var name = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                    var path = Path.Combine(folder, name);

                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await img.CopyToAsync(stream);
                    }

                    inventario.Fotos.Add(new Foto
                    {
                        InventarioCodigo = inventario.Codigo,
                        Ruta = "/images/inventarios/" + name
                    });
                }
            }

            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            var usuario = User.Identity?.Name ?? "Desconocido";

            await Guardar(
                inventario.Codigo,
                "CREAR",
                usuario,
                "Creación de producto",
                null,
                new
                {
                    inventario.Codigo,
                    inventario.Marca,
                    inventario.SKU,
                    inventario.PrecioVenta,
                    inventario.StockTotal
                }
            );

            return RedirectToAction(nameof(Index));
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string codigo)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Fotos)
                .Include(i => i.Tallas)
                .FirstOrDefaultAsync(i => i.Codigo == codigo);

            if (inventario == null)
                return NotFound();

            var codigoSeguro = inventario.Codigo;

            await Guardar(
                codigoSeguro,
                "ELIMINAR",
                User.Identity?.Name ?? "Sistema",
                "Eliminación de producto",
                new
                {
                    inventario.Codigo,
                    inventario.Marca,
                    inventario.PrecioVenta,
                    inventario.StockTotal
                },
                null
            );

            foreach (var foto in inventario.Fotos)
            {
                var ruta = Path.Combine(
                    _environment.WebRootPath,
                    foto.Ruta.Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(ruta))
                    System.IO.File.Delete(ruta);
            }

            _context.Inventarios.Remove(inventario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // EDIT GET
        public async Task<IActionResult> Edit(string id)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == id);

            if (inventario == null)
                return NotFound();

            ViewBag.Proveedores = _context.ProveedoresCatalogo
                .Where(p => p.Activo)
                .ToList();

            return View(inventario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Inventario model, List<IFormFile> nuevasFotos)
        {
            ModelState.Remove("Tallas");

            if (!ModelState.IsValid)
                return View(model);

            var inventarioAntes = await _context.Inventarios
                .AsNoTracking()
                .Include(i => i.Tallas)
                .FirstAsync(i => i.Codigo == model.Codigo);

            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .FirstAsync(i => i.Codigo == model.Codigo);

            inventario.Marca = model.Marca;
            inventario.SKU = model.SKU;
            inventario.CodigoCabys = model.CodigoCabys;
            inventario.PrecioCosto = model.PrecioCosto;
            inventario.PrecioVenta = model.PrecioVenta;
            inventario.Empresa = model.Empresa;
            inventario.ProveedorCatalogoId = model.ProveedorCatalogoId;


            _context.TallasInventario.RemoveRange(inventario.Tallas);
            inventario.Tallas = new List<TallaInventario>();

            foreach (var talla in model.Tallas)
            {
                inventario.Tallas.Add(new TallaInventario
                {
                    InventarioCodigo = inventario.Codigo,
                    Numero = talla.Numero,
                    Cantidad = talla.Cantidad,
                    Color = talla.Color,
                    Detalle = talla.Detalle ?? "",
                    Precio = talla.Precio > 0 ? talla.Precio : inventario.PrecioVenta
                });
            }

            if (nuevasFotos != null && nuevasFotos.Any())
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "inventarios");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (var img in nuevasFotos)
                {
                    var name = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                    var path = Path.Combine(folder, name);

                    using var stream = new FileStream(path, FileMode.Create);
                    await img.CopyToAsync(stream);

                    inventario.Fotos.Add(new Foto
                    {
                        InventarioCodigo = inventario.Codigo,
                        Ruta = "/images/inventarios/" + name
                    });
                }
            }

            await _context.SaveChangesAsync();

            var usuario = User.Identity?.Name ?? "Desconocido";

            await Guardar(
                inventario.Codigo,
                "EDITAR",
                usuario,
                "Edición de producto",
                new
                {
                    inventarioAntes.Codigo,
                    inventarioAntes.Marca,
                    inventarioAntes.PrecioVenta,
                    inventarioAntes.StockTotal
                },
                new
                {
                    inventario.Codigo,
                    inventario.Marca,
                    inventario.PrecioVenta,
                    inventario.StockTotal
                }
            );

            return RedirectToAction(nameof(Index));
        }

        // UPDATE STOCK
        [HttpPost]
        public async Task<IActionResult> UpdateStock(string Codigo, List<TallaInventario> Tallas)
        {
            foreach (var t in Tallas)
            {
                if (t.Id == 0)
                {
                    t.InventarioCodigo = Codigo;
                    _context.TallasInventario.Add(t);
                }
                else
                {
                    var tallaDb = await _context.TallasInventario.FindAsync(t.Id);

                    if (tallaDb != null)
                    {
                        tallaDb.Color = t.Color;
                        tallaDb.Detalle = t.Detalle;
                        tallaDb.Numero = t.Numero;
                        tallaDb.Cantidad = t.Cantidad;
                    }
                }
            }

            await _context.SaveChangesAsync();

            var usuario = User.Identity?.Name ?? "Desconocido";

            await Guardar(
                Codigo,
                "EDITAR_STOCK",
                usuario,
                "Actualización de stock",
                null,
                Tallas.Select(t => new
                {
                    t.Numero,
                    t.Cantidad,
                    t.Color
                })
            );

            return RedirectToAction("Details", new { id = Codigo });
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .Include(i => i.Proveedor)
                .FirstOrDefaultAsync(i => i.Codigo == id);

            if (inventario == null)
                return NotFound();

            return View(inventario);
        }

        // HISTORIAL
        public async Task Guardar(string codigo, string accion, string usuario, string? motivo, object? antes, object? despues)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(codigo))
                    return;

                var historial = new HistorialInventario
                {
                    CodigoInventario = codigo,
                    Accion = accion,
                    Usuario = usuario,
                    Fecha = DateTime.UtcNow,
                    Motivo = motivo,
                    DatosAntes = antes != null ? JsonSerializer.Serialize(antes) : null,
                    DatosDespues = despues != null ? JsonSerializer.Serialize(despues) : null
                };

                _context.HistorialInventarios.Add(historial);
                await _context.SaveChangesAsync();
            }
            catch
            {
                // no rompe sistema
            }
        }

        // DELETE MULTIPLE - CONFIRMAR
        [HttpGet]
        public async Task<IActionResult> DeleteMultiple(List<string> codigos)
        {
            if (codigos == null || !codigos.Any())
                return RedirectToAction(nameof(Index));

            var inventarios = await _context.Inventarios
                .Where(i => codigos.Contains(i.Codigo))
                .ToListAsync();

            return View(inventarios);
        }

        // DELETE MULTIPLE - EJECUTAR
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMultipleConfirmed(List<string> codigos)
        {
            if (codigos == null || !codigos.Any())
                return RedirectToAction(nameof(Index));

            var inventarios = await _context.Inventarios
                .Where(i => codigos.Contains(i.Codigo))
                .Include(i => i.Fotos)
                .ToListAsync();

            foreach (var inv in inventarios)
            {
                foreach (var foto in inv.Fotos)
                {
                    var ruta = Path.Combine(
                        _environment.WebRootPath,
                        foto.Ruta.Replace("/", Path.DirectorySeparatorChar.ToString())
                    );

                    if (System.IO.File.Exists(ruta))
                        System.IO.File.Delete(ruta);
                }
            }

            _context.Inventarios.RemoveRange(inventarios);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public IActionResult GraficoStockPorMarca()
        {
            var inventarios = _context.Inventarios
                .Include(i => i.Tallas)
                .ToList();

            var stockPorMarca = inventarios
                .GroupBy(i => i.Marca)
                .Select(g => new
                {
                    Marca = g.Key,
                    Stock = g.Sum(i => i.StockTotal)
                })
                .OrderByDescending(x => x.Stock)
                .ToList();

            var stockPorEmpresa = inventarios
                .GroupBy(i => i.Empresa)
                .Select(g => new
                {
                    Empresa = g.Key,
                    Stock = g.Sum(i => i.StockTotal)
                })
                .OrderByDescending(x => x.Stock)
                .ToList();

            ViewBag.Marcas = stockPorMarca.Select(x => x.Marca).ToList();
            ViewBag.StockMarca = stockPorMarca.Select(x => x.Stock).ToList();

            ViewBag.Empresas = stockPorEmpresa.Select(x => x.Empresa).ToList();
            ViewBag.StockEmpresa = stockPorEmpresa.Select(x => x.Stock).ToList();

            return View();
        }

        [HttpGet]
        public IActionResult BuscarProveedores(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var proveedores = _context.ProveedoresCatalogo
                .Where(p =>
                    p.Nombre.Contains(term) ||
                    p.Codigo.Contains(term) ||
                    p.Cedula.Contains(term)
                )
                .Take(10)
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre,
                    codigo = p.Codigo
                })
                .ToList();

            return Json(proveedores);
        }

        //DELETE FOTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoto(int id, string inventarioCodigo)
        {
            var foto = await _context.Fotos.FindAsync(id);

            if (foto == null)
                return NotFound();

            // Eliminar archivo físico
            if (!string.IsNullOrEmpty(foto.Ruta))
            {
                var rutaFisica = Path.Combine(
                    _environment.WebRootPath,
                    foto.Ruta.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (System.IO.File.Exists(rutaFisica))
                {
                    System.IO.File.Delete(rutaFisica);
                }
            }

            _context.Fotos.Remove(foto);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = inventarioCodigo });
        }



    }
}
