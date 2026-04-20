using FashionM.Data;
using FashionM.Models.Provedor;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

public class ProveedorCatalogoController : Controller
{
    private readonly AppDbContext _context;

    public ProveedorCatalogoController(AppDbContext context)
    {
        _context = context;
    }

    // =========================
    // INDEX
    // =========================
    public async Task<IActionResult> Index(string busqueda, bool? activo, int page = 1)
    {
        int pageSize = 25;

        var query = _context.ProveedoresCatalogo
            .Include(p => p.Zapatos)
            .AsQueryable();

        // 🔍 BUSQUEDA
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(p =>
                p.Codigo.Contains(busqueda) ||
                p.Nombre.Contains(busqueda)
            );
        }

        // 🔍 ESTADO
        if (activo.HasValue)
        {
            query = query.Where(p => p.Activo == activo.Value);
        }

        int totalItems = await query.CountAsync();

        var proveedores = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;
        ViewBag.Busqueda = busqueda;
        ViewBag.Activo = activo;

        return View(proveedores);
    }

    // =========================
    // CREATE PROVEEDOR (GET)
    // =========================
    public IActionResult Create()
    {
        return View();
    }

    // =========================
    // CREATE PROVEEDOR (POST)
    // =========================
    [HttpPost]
    public async Task<IActionResult> Create(ProveedorCatalogo model)
    {
        if (!ModelState.IsValid)
            return View(model);

        _context.ProveedoresCatalogo.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = model.Id });
    }

    // =========================
    // DETAILS PROVEEDOR
    // =========================
    public async Task<IActionResult> Details(
    int id,
    string busqueda,
    string empresa,
    DateTime? fechaDesde,
    DateTime? fechaHasta,
    int page = 1)
    {
        int pageSize = 75;

        var proveedor = await _context.ProveedoresCatalogo
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proveedor == null)
            return NotFound();

        var query = _context.ZapatosProveedor
            .Where(z => z.ProveedorCatalogoId == id)
            .Include(z => z.Colores)
            .Include(z => z.Suelas)
            .Include(z => z.Detalles)
            .Include(z => z.Tallas)
            .AsQueryable();

        // 🔍 BUSQUEDA
        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            query = query.Where(z => z.Codigo.Contains(busqueda));
        }

        // 🔍 EMPRESA
        if (!string.IsNullOrWhiteSpace(empresa))
        {
            query = query.Where(z => z.Empresa == empresa);
        }

        // 🔥 FECHAS UTC (POSTGRES)
        if (fechaDesde.HasValue)
        {
            var desdeUtc = DateTime.SpecifyKind(fechaDesde.Value, DateTimeKind.Utc);
            query = query.Where(z => z.FechaIngreso >= desdeUtc);
        }

        if (fechaHasta.HasValue)
        {
            var hastaUtc = DateTime.SpecifyKind(fechaHasta.Value, DateTimeKind.Utc)
                .AddDays(1).AddTicks(-1);

            query = query.Where(z => z.FechaIngreso <= hastaUtc);
        }

        int totalItems = await query.CountAsync();

        var zapatos = await query
            .OrderByDescending(z => z.FechaIngreso)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        proveedor.Zapatos = zapatos;

        ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        ViewBag.CurrentPage = page;

        ViewBag.Busqueda = busqueda;
        ViewBag.Empresa = empresa;
        ViewBag.FechaDesde = fechaDesde;
        ViewBag.FechaHasta = fechaHasta;

        return View(proveedor);
    }

    // =========================
    // Editar PROVEEDOR
    // =========================
    public async Task<IActionResult> Edit(int id)
    {
        var proveedor = await _context.ProveedoresCatalogo.FindAsync(id);

        if (proveedor == null)
            return NotFound();

        return View(proveedor);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(ProveedorCatalogo model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var proveedorDb = await _context.ProveedoresCatalogo.FindAsync(model.Id);

        if (proveedorDb == null)
            return NotFound();

        proveedorDb.Nombre = model.Nombre;
        proveedorDb.Codigo = model.Codigo;
        proveedorDb.Cedula = model.Cedula;
        proveedorDb.Telefonos = model.Telefonos;
        proveedorDb.Direccion = model.Direccion;
        proveedorDb.Correo = model.Correo;
        proveedorDb.ActividadEconomica = model.ActividadEconomica;
        proveedorDb.Activo = model.Activo;

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = model.Id });
    }

    // =========================
    // Delete PROVEEDOR
    // =========================
    public async Task<IActionResult> Delete(int id)
    {
        var proveedor = await _context.ProveedoresCatalogo
            .Include(p => p.Zapatos)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proveedor == null)
            return NotFound();

        return View(proveedor);
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var proveedor = await _context.ProveedoresCatalogo
            .Include(p => p.Zapatos)
                .ThenInclude(z => z.Colores)
            .Include(p => p.Zapatos)
                .ThenInclude(z => z.Suelas)
            .Include(p => p.Zapatos)
                .ThenInclude(z => z.Detalles)
            .Include(p => p.Zapatos)
                .ThenInclude(z => z.Tallas)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (proveedor == null)
            return NotFound();

        // 🔥 ELIMINAR IMÁGENES
        foreach (var zapato in proveedor.Zapatos)
        {
            if (!string.IsNullOrEmpty(zapato.ImagenUrl))
            {
                var rutaImagen = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    zapato.ImagenUrl.TrimStart('/')
                );

                if (System.IO.File.Exists(rutaImagen))
                {
                    System.IO.File.Delete(rutaImagen);
                }
            }

            _context.RemoveRange(zapato.Colores);
            _context.RemoveRange(zapato.Suelas);
            _context.RemoveRange(zapato.Detalles);
            _context.RemoveRange(zapato.Tallas);
        }

        _context.RemoveRange(proveedor.Zapatos);
        _context.ProveedoresCatalogo.Remove(proveedor);

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }



    // =========================
    // CREATE ZAPATO (GET)
    // =========================
    public IActionResult CreateZapato(int proveedorId)
    {
        var model = new ZapatoProveedor
        {
            ProveedorCatalogoId = proveedorId
        };

        return View(model);
    }

    // =========================
    // CREATE ZAPATO (POST)
    // =========================
    [HttpPost]
    public async Task<IActionResult> CreateZapato(ZapatoProveedor model, IFormFile? imagen)
    {
        if (!ModelState.IsValid)
            return View(model);

        // =========================
        // GUARDAR IMAGEN
        // =========================
        if (imagen != null && imagen.Length > 0)
        {
            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);

            var ruta = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot/imagenes/ZapatosProveedor", nombreArchivo);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            model.ImagenUrl = "/imagenes/ZapatosProveedor/" + nombreArchivo;
        }

        _context.ZapatosProveedor.Add(model);
        await _context.SaveChangesAsync();

        return RedirectToAction("EditZapato", new { id = model.Id });
    }

    // =========================
    // EDIT ZAPATO (GET)
    // =========================
    public async Task<IActionResult> EditZapato(int id)
    {
        var zapato = await _context.ZapatosProveedor
            .Include(z => z.Colores)
            .Include(z => z.Suelas)
            .Include(z => z.Detalles)
            .Include(z => z.Tallas)
            .FirstOrDefaultAsync(z => z.Id == id);

        if (zapato == null)
            return NotFound();

        return View(zapato);
    }

    // =========================
    // EDIT ZAPATO (POST)
    // =========================
    [HttpPost]
    public async Task<IActionResult> EditZapato(ZapatoProveedor model, IFormFile? imagen)
    {
        if (!ModelState.IsValid)
            return View(model);

        var zapatoDb = await _context.ZapatosProveedor
            .Include(z => z.Colores)
            .Include(z => z.Suelas)
            .Include(z => z.Detalles)
            .Include(z => z.Tallas)
            .FirstOrDefaultAsync(z => z.Id == model.Id);

        if (zapatoDb == null)
            return NotFound();

        // =========================
        // ACTUALIZAR BASE
        // =========================
        zapatoDb.Codigo = model.Codigo;
        zapatoDb.Empresa = model.Empresa;
        zapatoDb.PrecioVenta = model.PrecioVenta;
        zapatoDb.PrecioCosto = model.PrecioCosto;
        zapatoDb.PrecioColombia = model.PrecioColombia;

        // =========================
        // IMAGEN
        // =========================
        if (imagen != null && imagen.Length > 0)
        {
            var nombreArchivo = Guid.NewGuid().ToString() + Path.GetExtension(imagen.FileName);

            var ruta = Path.Combine(Directory.GetCurrentDirectory(),
                "wwwroot/imagenes/ZapatosProveedor", nombreArchivo);

            using (var stream = new FileStream(ruta, FileMode.Create))
            {
                await imagen.CopyToAsync(stream);
            }

            zapatoDb.ImagenUrl = "/imagenes/ZapatosProveedor/" + nombreArchivo;
        }

        // =========================
        // REEMPLAZAR LISTAS
        // =========================
        zapatoDb.Colores.Clear();
        zapatoDb.Suelas.Clear();
        zapatoDb.Detalles.Clear();
        zapatoDb.Tallas.Clear();

        foreach (var c in model.Colores ?? new List<ColorZapato>())
            zapatoDb.Colores.Add(new ColorZapato { Nombre = c.Nombre });

        foreach (var s in model.Suelas ?? new List<SuelaZapato>())
            zapatoDb.Suelas.Add(new SuelaZapato { Nombre = s.Nombre });

        foreach (var d in model.Detalles ?? new List<DetalleZapato>())
            zapatoDb.Detalles.Add(new DetalleZapato { Nombre = d.Nombre });

        foreach (var t in model.Tallas ?? new List<TallaZapato>())
            zapatoDb.Tallas.Add(new TallaZapato
            {
                Numero = t.Numero,
                Precio = t.Precio
            });

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = zapatoDb.ProveedorCatalogoId });
    }

    // =========================
    // DELETE ZAPATO
    // =========================
    public async Task<IActionResult> DeleteZapato(int id)
    {
        var zapato = await _context.ZapatosProveedor
            .Include(z => z.Proveedor)
            .FirstOrDefaultAsync(z => z.Id == id);

        if (zapato == null)
            return NotFound();

        return View(zapato);
    }

    [HttpPost, ActionName("DeleteZapato")]
    public async Task<IActionResult> DeleteZapatoConfirmed(int id)
    {
        var zapato = await _context.ZapatosProveedor
            .Include(z => z.Colores)
            .Include(z => z.Suelas)
            .Include(z => z.Detalles)
            .Include(z => z.Tallas)
            .FirstOrDefaultAsync(z => z.Id == id);

        if (zapato == null)
            return NotFound();

        // 🔥 ELIMINAR IMAGEN
        if (!string.IsNullOrEmpty(zapato.ImagenUrl))
        {
            var ruta = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                zapato.ImagenUrl.TrimStart('/')
            );

            if (System.IO.File.Exists(ruta))
            {
                System.IO.File.Delete(ruta);
            }
        }

        // 🔥 ELIMINAR RELACIONES
        _context.RemoveRange(zapato.Colores);
        _context.RemoveRange(zapato.Suelas);
        _context.RemoveRange(zapato.Detalles);
        _context.RemoveRange(zapato.Tallas);

        _context.ZapatosProveedor.Remove(zapato);

        await _context.SaveChangesAsync();

        return RedirectToAction("Details", new { id = zapato.ProveedorCatalogoId });
    }



}
