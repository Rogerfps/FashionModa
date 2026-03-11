using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
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

            var query = _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .AsQueryable();

            // 🔎 Buscador
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Codigo.Contains(search) ||
                    i.Marca.Contains(search) ||
                    i.SKU.Contains(search) ||
                    i.Color.Contains(search)
                );
            }

            // 🏢 Filtro empresa
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

            // lista de empresas para dropdown
            ViewBag.Empresas = _context.Inventarios
                .Select(i => i.Empresa)
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            return View(inventarios);
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Inventario inventario,
            List<IFormFile> imagenes)
        {
            inventario.Fotos ??= new List<Foto>();
            inventario.Tallas ??= new List<TallaInventario>();

            // validar tallas
            if (!inventario.Tallas.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos una talla");
            }

            // validar codigo duplicado
            if (_context.Inventarios.Any(i => i.Codigo == inventario.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un inventario con este código");
            }

            if (!ModelState.IsValid)
                return View(inventario);

            // asignar FK a tallas
            foreach (var talla in inventario.Tallas)
            {
                talla.InventarioCodigo = inventario.Codigo;
            }

            // guardar imagenes
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

            return RedirectToAction(nameof(Index));
        }

        // DELETE INVENTARIO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string codigo)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == codigo);

            if (inventario == null)
                return NotFound();

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

            return View(inventario);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Inventario model, List<IFormFile> nuevasFotos)
        {
            if (!ModelState.IsValid)
                return View(model);

            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .FirstAsync(i => i.Codigo == model.Codigo);

            inventario.Marca = model.Marca;
            inventario.Color = model.Color;
            inventario.Detalle = model.Detalle;
            inventario.SKU = model.SKU;
            inventario.CodigoCabys = model.CodigoCabys;
            inventario.PrecioCosto = model.PrecioCosto;
            inventario.PrecioVenta = model.PrecioVenta;
            inventario.Empresa = model.Empresa;

            _context.TallasInventario.RemoveRange(inventario.Tallas);

            inventario.Tallas = new List<TallaInventario>();

            foreach (var talla in model.Tallas)
            {
                inventario.Tallas.Add(new TallaInventario
                {
                    InventarioCodigo = inventario.Codigo,
                    Numero = talla.Numero,
                    Cantidad = talla.Cantidad
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

            return RedirectToAction(nameof(Index));
        }

        // DETAILS
        public async Task<IActionResult> Details(string id)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == id);

            if (inventario == null)
                return NotFound();

            return View(inventario);
        }

        // DELETE FOTO
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoto(int id, string inventarioCodigo)
        {
            var foto = await _context.Fotos.FindAsync(id);

            if (foto == null)
                return NotFound();

            var rutaFisica = Path.Combine(
                _environment.WebRootPath,
                foto.Ruta.TrimStart('/')
            );

            if (System.IO.File.Exists(rutaFisica))
                System.IO.File.Delete(rutaFisica);

            _context.Fotos.Remove(foto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = inventarioCodigo });
        }
    }
}