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

        // GET: Inventarios
        public IActionResult Index(string search, int page = 1)
        {
            int pageSize = 6;

            var query = _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .AsQueryable();

            // 🔍 Buscador
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(i =>
                    i.Codigo.ToString().Contains(search) ||
                    i.Marca.Contains(search) ||
                    i.SKU.Contains(search) ||
                    i.Color.Contains(search)
                );
            }

            // 🔢 Total de registros
            int totalItems = query.Count();

            // 📄 Paginación REAL
            var inventarios = query
                .OrderBy(i => i.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // 📦 Datos para la vista
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            ViewBag.Search = search;

            return View(inventarios);
        }

        // GET: Inventarios/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Inventarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Inventario inventario,
            List<IFormFile> imagenes)
        {
            if (inventario.Tallas == null || !inventario.Tallas.Any())
            {
                ModelState.AddModelError("", "Debe agregar al menos una talla");
            }

            if (!ModelState.IsValid)
            {
                return View(inventario);
            }

            if (_context.Inventarios.Any(i => i.Codigo == inventario.Codigo))
            {
                ModelState.AddModelError("Codigo", "Ya existe un inventario con este código");
                return View(inventario);
            }

            foreach (var talla in inventario.Tallas)
            {
                talla.InventarioCodigo = inventario.Codigo;
                talla.Inventario = inventario;
            }

            if (imagenes != null && imagenes.Any())
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "inventarios");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                foreach (var img in imagenes)
                {
                    if (img.Length == 0) continue;

                    var name = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                    var path = Path.Combine(folder, name);

                    using var stream = new FileStream(path, FileMode.Create);
                    await img.CopyToAsync(stream);

                    inventario.Fotos.Add(new Foto
                    {
                        Ruta = "/images/inventarios/" + name
                    });
                }
            }

            _context.Inventarios.Add(inventario);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int codigo)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == codigo);

            if (inventario == null)
                return NotFound();

            //  borrar imagenes físicas
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
        //Editar
        public async Task<IActionResult> Edit(int id)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == id);

            if (inventario == null)
                return NotFound();

            return View(inventario);
        }

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

            // 🔹 CAMPOS SIMPLES
            inventario.Marca = model.Marca;
            inventario.Color = model.Color;
            inventario.Detalle = model.Detalle;
            inventario.SKU = model.SKU;
            inventario.CodigoCabys = model.CodigoCabys;
            inventario.PrecioCosto = model.PrecioCosto;
            inventario.PrecioVenta = model.PrecioVenta;

            // 🔹 TALLAS (BORRAR Y VOLVER A INSERTAR)
            _context.TallasInventario.RemoveRange(inventario.Tallas);

            foreach (var talla in model.Tallas)
            {
                inventario.Tallas.Add(new TallaInventario
                {
                    Numero = talla.Numero,
                    Cantidad = talla.Cantidad
                });
            }

            // 🔹 NUEVAS FOTOS
            if (nuevasFotos != null && nuevasFotos.Any())
            {
                var folder = Path.Combine(_environment.WebRootPath, "images", "inventarios");

                foreach (var img in nuevasFotos)
                {
                    var name = $"{Guid.NewGuid()}{Path.GetExtension(img.FileName)}";
                    var path = Path.Combine(folder, name);

                    using var stream = new FileStream(path, FileMode.Create);
                    await img.CopyToAsync(stream);

                    inventario.Fotos.Add(new Foto
                    {
                        Ruta = "/images/inventarios/" + name
                    });
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //Details

        public async Task<IActionResult> Details(int id)
        {
            var inventario = await _context.Inventarios
                .Include(i => i.Tallas)
                .Include(i => i.Fotos)
                .FirstOrDefaultAsync(i => i.Codigo == id);

            if (inventario == null)
                return NotFound();

            return View(inventario);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFoto(int id, int inventarioCodigo)
        {
            var foto = await _context.Fotos.FindAsync(id);

            if (foto == null)
                return NotFound();

            // borrar archivo físico
            var rutaFisica = Path.Combine(
                _environment.WebRootPath,
                foto.Ruta.TrimStart('/')
            );

            if (System.IO.File.Exists(rutaFisica))
            {
                System.IO.File.Delete(rutaFisica);
            }

            _context.Fotos.Remove(foto);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = inventarioCodigo });
        }
    }
}   