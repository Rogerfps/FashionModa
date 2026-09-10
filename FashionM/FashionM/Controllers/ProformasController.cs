using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class ProformasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProformasController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ==========================
        // LISTADO
        // ==========================
        public async Task<IActionResult> Index(string buscar, int? empresaId)
        {

            // Si no viene el parámetro empresa, usar la empresa seleccionada
            if (!Request.Query.ContainsKey("empresaId"))
            {
                empresaId = HttpContext.Session.GetInt32("EmpresaId");

                if (!empresaId.HasValue)
                {
                    return RedirectToAction("SeleccionarEmpresa", "Home");
                }
            }

            var query = _context.Proformas
                .Include(p => p.Empresa)
                .Include(p => p.Cliente)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                buscar = buscar.ToLower();

                query = query.Where(p =>
                    p.Numero.ToString().Contains(buscar) 
                    || p.Cliente.Nombre.ToLower().Contains(buscar)
                    || p.Cliente.Apellidos.ToLower().Contains(buscar)
                    || p.Cliente.Codigo.ToLower().Contains(buscar)
                );
            }

            if (empresaId.HasValue && empresaId.Value != 0)
            {
                query = query.Where(p => p.EmpresaId == empresaId.Value);
            }

            var proformas = await query
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            ViewBag.Empresas = await _context.Empresas.ToListAsync();
            ViewBag.Buscar = buscar;
            ViewBag.EmpresaId = empresaId;

            return View(proformas);
        }

        // ==========================
        // CREATE GET
        // ==========================
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Empresas = new SelectList(_context.Empresas, "Id", "Nombre");
            return View(new Proforma());
        }

        // ==========================
        // CREATE POST
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Proforma proforma)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Empresas = new SelectList(_context.Empresas, "Id", "Nombre", proforma.EmpresaId);
                return View(proforma);
            }

            var ultimoNumero = await _context.Proformas
                .Where(p => p.EmpresaId == proforma.EmpresaId)
                .MaxAsync(p => (int?)p.Numero) ?? 0;

            proforma.Numero = ultimoNumero + 1;
            proforma.Fecha = DateTime.UtcNow;

            _context.Add(proforma);
            await _context.SaveChangesAsync();

            return RedirectToAction("AgregarProducto", new { id = proforma.Id });
        }

        // ==========================
        // AGREGAR PRODUCTOS (GET)
        // ==========================
        public async Task<IActionResult> AgregarProducto(int id)
        {
            var proforma = await _context.Proformas
                .Include(p => p.Detalles)
                .Include(p => p.Cliente)
                .Include(p => p.Empresa)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proforma == null)
                return NotFound();

            return View(proforma);
        }

        // ==========================
        // AGREGAR PRODUCTO (POST)
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearZapato(
            int proformaId,
            string codigo,
            string color,
            string[] tallas,
            int[] cantidades,
            decimal[] preciosVenta,
            decimal descuentoProducto = 0)
        {
            if (tallas == null || cantidades == null || preciosVenta == null)
            {
                TempData["Error"] = "Debe seleccionar al menos una talla.";
                return RedirectToAction(nameof(AgregarProducto), new { id = proformaId });
            }

            if (tallas.Length != cantidades.Length || tallas.Length != preciosVenta.Length)
            {
                TempData["Error"] = "Los datos de las tallas no coinciden.";
                return RedirectToAction(nameof(AgregarProducto), new { id = proformaId });
            }

            if (descuentoProducto < 0 || descuentoProducto > 100)
            {
                TempData["Error"] = "El descuento debe estar entre 0% y 100%.";
                return RedirectToAction(nameof(AgregarProducto), new { id = proformaId });
            }

            var proforma = await _context.Proformas
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == proformaId);

            if (proforma == null)
                return NotFound();

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                for (int i = 0; i < tallas.Length; i++)
                {
                    if (cantidades[i] <= 0)
                        continue;

                    var inventario = await _context.TallasInventario
                        .FirstOrDefaultAsync(t =>
                            t.InventarioCodigo == codigo &&
                            t.Color == color &&
                            t.Numero == tallas[i]);

                    if (inventario == null)
                    {
                        TempData["Error"] =
                            $"No se encontró inventario para {codigo}, color {color}, talla {tallas[i]}.";

                        await transaction.RollbackAsync();

                        return RedirectToAction(nameof(AgregarProducto),
                            new { id = proformaId });
                    }

                    if (inventario.Cantidad < cantidades[i])
                    {
                        TempData["Error"] =
                            $"Stock insuficiente para {codigo}, talla {tallas[i]}. " +
                            $"Disponible: {inventario.Cantidad}.";

                        await transaction.RollbackAsync();

                        return RedirectToAction(nameof(AgregarProducto),
                            new { id = proformaId });
                    }

                    decimal precio = preciosVenta[i];

                    decimal subtotalBruto =
                        cantidades[i] * precio;

                    decimal descuentoMonto =
                        Math.Round(
                            subtotalBruto * (descuentoProducto / 100m),
                            2);

                    decimal subtotalFinal =
                        subtotalBruto - descuentoMonto;

                    // Restar del inventario
                    inventario.Cantidad -= cantidades[i];

                    var detalle = new ProformaDetalle
                    {
                        ProformaId = proformaId,
                        InventarioCodigo = codigo,
                        CodigoProducto = codigo,
                        Color = color,
                        Talla = tallas[i],
                        Cantidad = cantidades[i],
                        PrecioUnitario = precio,

                        // Descuento aplicado a esta línea
                        DescuentoPorcentaje = descuentoProducto,
                        DescuentoMonto = descuentoMonto,

                        // Subtotal después del descuento del producto
                        SubTotal = subtotalFinal,

                        CantidadDevuelta = 0
                    };

                    _context.ProformaDetalles.Add(detalle);
                }

                await _context.SaveChangesAsync();

                // Recalcular todos los totales
                await RecalcularTotalesProforma(proformaId);

                await transaction.CommitAsync();

                return RedirectToAction(nameof(AgregarProducto),
                    new { id = proformaId });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Ocurrió un error al agregar el producto.";

                return RedirectToAction(nameof(AgregarProducto),
                    new { id = proformaId });
            }
        }

        private async Task RecalcularTotalesProforma(int proformaId)
        {
            var proforma = await _context.Proformas
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == proformaId);

            if (proforma == null)
                return;

            decimal subtotalProforma = 0;

            foreach (var detalle in proforma.Detalles)
            {
                int cantidadActual =
                    detalle.Cantidad - detalle.CantidadDevuelta;

                if (cantidadActual < 0)
                    cantidadActual = 0;

                decimal subtotalBruto =
                    cantidadActual * detalle.PrecioUnitario;

                decimal descuentoDetalle =
                    Math.Round(
                        subtotalBruto *
                        (detalle.DescuentoPorcentaje / 100m),
                        2);

                detalle.DescuentoMonto = descuentoDetalle;

                detalle.SubTotal =
                    subtotalBruto - descuentoDetalle;

                subtotalProforma += detalle.SubTotal;
            }

            // Descuento general aplicado DESPUÉS
            // del descuento individual
            proforma.DescuentoMonto =
                Math.Round(
                    subtotalProforma *
                    (proforma.DescuentoPorcentaje / 100m),
                    2);

            proforma.Total =
                subtotalProforma -
                proforma.DescuentoMonto;

            await _context.SaveChangesAsync();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AplicarDescuentoGeneral(
    int proformaId,
    decimal descuentoPorcentaje)
        {
            var proforma = await _context.Proformas
                .FirstOrDefaultAsync(p => p.Id == proformaId);

            if (proforma == null)
                return NotFound();

            if (descuentoPorcentaje < 0 ||
                descuentoPorcentaje > 100)
            {
                TempData["Error"] =
                    "El descuento debe estar entre 0% y 100%.";

                return RedirectToAction(nameof(AgregarProducto),
                    new { id = proformaId });
            }

            proforma.DescuentoPorcentaje =
                descuentoPorcentaje;

            await _context.SaveChangesAsync();

            await RecalcularTotalesProforma(proformaId);

            return RedirectToAction(nameof(AgregarProducto),
                new { id = proformaId });
        }

        // ==========================
        // ACTUALIZAR TOTAL
        // ==========================
        private async Task ActualizarTotal(int proformaId)
        {
            await RecalcularTotalesProforma(proformaId);
        }

        private async Task CrearActualizarVenta(int proformaId)
        {
            // ========================================
            // PROFORMA
            // ========================================

            var proforma = await _context.Proformas
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p =>
                    p.Id == proformaId);

            if (proforma == null)
                return;

            // ========================================
            // BUSCAR VENTA
            // ========================================

            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v =>

                    v.DocumentoId == proforma.Id &&

                    v.TipoDocumento == "PROFORMA"
                );

            // ========================================
            // CREAR
            // ========================================
            int cantidadZapatos = proforma.Detalles.Sum(x => x.Cantidad);


            if (venta == null)
            {
                venta = new Venta
                {
                    Fecha = proforma.Fecha,

                    TipoDocumento = "PROFORMA",

                    DocumentoId = proforma.Id,

                    ClienteCedula = proforma.ClienteCedula,

                    EmpresaId = proforma.EmpresaId,

                    Total = proforma.Total,

                    NumeroCajas = proforma.NumeroCajas,

                    CantidadZapatos = cantidadZapatos,

                    FacturadoPor = proforma.FacturadoPor,

                    AgenteVenta = proforma.AgenteVenta,

                    Estado = "ACTIVA",

                    Semana =
                        ISOWeek.GetWeekOfYear(
                            proforma.Fecha
                        ),

                    Mes =
                        proforma.Fecha.Month,

                    Año =
                        proforma.Fecha.Year
                };

                _context.Ventas.Add(venta);

                await _context.SaveChangesAsync();

                // 🔥 RECARGAR ID REAL
                await _context.Entry(venta)
                    .ReloadAsync();
            }
            else
            {
                // ========================================
                // ACTUALIZAR
                // ========================================

                venta.Total =
                    proforma.Total;

                venta.NumeroCajas =
                    proforma.NumeroCajas;

                venta.CantidadZapatos = proforma.Detalles.Sum(x => x.Cantidad);

                venta.AgenteVenta =
                    proforma.AgenteVenta;

                venta.Semana =
                    ISOWeek.GetWeekOfYear(
                        proforma.Fecha
                    );

                venta.Mes =
                    proforma.Fecha.Month;

                venta.Año =
                    proforma.Fecha.Year;

                await _context.SaveChangesAsync();

                // ========================================
                // ELIMINAR DETALLES
                // ========================================

                var detallesViejos =
                    await _context.VentaDetalles
                        .Where(x =>
                            x.VentaId == venta.Id)
                        .ToListAsync();

                _context.VentaDetalles
                    .RemoveRange(detallesViejos);

                await _context.SaveChangesAsync();
            }

            // ========================================
            // LIMPIAR TRACKER
            // ========================================

            _context.ChangeTracker.Clear();

            // ========================================
            // RECARGAR VENTA LIMPIA
            // ========================================

            venta = await _context.Ventas
                .FirstOrDefaultAsync(v =>
                    v.Id == venta.Id);

            if (venta == null)
                return;

            // ========================================
            // AGREGAR DETALLES
            // ========================================

            foreach (var item in proforma.Detalles)
            {
                var detalle =
                    new VentaDetalle
                    {
                        VentaId =
                            venta.Id,

                        InventarioCodigo =
                            item.InventarioCodigo,

                        Color =
                            item.Color,

                        Talla =
                            item.Talla,

                        Cantidad =
                            item.Cantidad,

                        PrecioUnitario =
                            item.PrecioUnitario,

                        SubTotal =
                            item.SubTotal
                    };

                _context.VentaDetalles
                    .Add(detalle);
            }

            await _context.SaveChangesAsync();
        }



        // ==========================
        // DETALLE
        // ==========================
        public async Task<IActionResult> Details(int id)
        {
            var proforma = await _context.Proformas
                .Include(p => p.Empresa)
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proforma == null)
                return NotFound();

            return View(proforma);
        }

        // ==========================
        // DELETE
        // ==========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var proforma = await _context.Proformas
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proforma == null)
                return NotFound();

            _context.Proformas.Remove(proforma);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ==========================
        // DEVOLVER PRODUCTO
        // ==========================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DevolverProducto(
            int detalleId,
            int cantidadDevuelta)
        {
            if (cantidadDevuelta <= 0)
            {
                TempData["Error"] =
                    "La cantidad a devolver debe ser mayor que cero.";

                return RedirectToAction(nameof(Index));
            }

            var detalle = await _context.ProformaDetalles
                .Include(d => d.Proforma)
                .FirstOrDefaultAsync(d => d.Id == detalleId);

            if (detalle == null)
                return NotFound();

            int cantidadDisponibleParaDevolver =
                detalle.Cantidad - detalle.CantidadDevuelta;

            if (cantidadDevuelta > cantidadDisponibleParaDevolver)
            {
                TempData["Error"] =
                    $"No puede devolver {cantidadDevuelta} unidades. " +
                    $"Solo quedan {cantidadDisponibleParaDevolver} unidades disponibles para devolución.";

                return RedirectToAction(
                    nameof(AgregarProducto),
                    new { id = detalle.ProformaId });
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var inventario = await _context.TallasInventario
                    .FirstOrDefaultAsync(t =>
                        t.InventarioCodigo == detalle.InventarioCodigo &&
                        t.Color == detalle.Color &&
                        t.Numero == detalle.Talla);

                if (inventario == null)
                {
                    TempData["Error"] =
                        "No se encontró el producto correspondiente en el inventario.";

                    await transaction.RollbackAsync();

                    return RedirectToAction(
                        nameof(AgregarProducto),
                        new { id = detalle.ProformaId });
                }

                inventario.Cantidad += cantidadDevuelta;

                detalle.CantidadDevuelta += cantidadDevuelta;

                await _context.SaveChangesAsync();

                await RecalcularTotalesProforma(
                    detalle.ProformaId);

                await transaction.CommitAsync();

                TempData["Success"] =
                    $"Se devolvieron {cantidadDevuelta} unidades al inventario.";

                return RedirectToAction(
                    nameof(AgregarProducto),
                    new { id = detalle.ProformaId });
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["Error"] =
                    "Ocurrió un error al devolver el producto.";

                return RedirectToAction(
                    nameof(AgregarProducto),
                    new { id = detalle.ProformaId });
            }
        }

        // ==========================
        // PDF
        // ==========================
        public async Task<IActionResult> GenerarPDF(int id)
        {
            var proforma = await _context.Proformas
                .Include(p => p.Empresa)
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proforma == null)
                return NotFound();

            // 🔽 NORMALIZAR NOMBRE
            string nombreEmpresa = proforma.Empresa.Nombre.ToLower().Trim();

            // 🔽 SELECCIONAR LOGO
            string logo = nombreEmpresa switch
            {
                "cocalza plus s.a" => "cocalza.png",
                "fashion shoes s.a" => "fashion.png",
                "lsg moda s.a" => "lsg.jpg",
                "maxiplus" => "maxiplus.png",
                "kyroz" => "KYROZ.png",
                _ => "default.png"
            };

            // 🔽 RUTA DEL LOGO
            var logoPath = Path.Combine(_env.WebRootPath, "images", logo);

            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = Path.Combine(_env.WebRootPath, "images", "default.png");
            }

            // 🔥 AGRUPAR DETALLES (AQUÍ ESTÁ LA MAGIA)
            var detallesAgrupados = proforma.Detalles
    .GroupBy(d => new
    {
        d.InventarioCodigo,
        d.Color,
        d.PrecioUnitario,
        d.DescuentoPorcentaje
    })
    .Select(g =>
    {
        int cantidadReal = g.Sum(x =>
            Math.Max(0, x.Cantidad - x.CantidadDevuelta));

        decimal subtotalBruto =
            cantidadReal * g.Key.PrecioUnitario;

        decimal descuentoProducto =
            Math.Round(
                subtotalBruto *
                (g.Key.DescuentoPorcentaje / 100m),
                2);

        decimal subtotal =
            subtotalBruto - descuentoProducto;

        return new
        {
            Codigo = g.Key.InventarioCodigo,
            Color = g.Key.Color,
            Cantidad = cantidadReal,
            PrecioUnitario = g.Key.PrecioUnitario,
            DescuentoPorcentaje = g.Key.DescuentoPorcentaje,
            DescuentoMonto = descuentoProducto,
            SubTotal = subtotal
        };
    })
    // No mostrar productos completamente devueltos
            .Where(x => x.Cantidad > 0)
            .ToList();

            var subtotalProductos = detallesAgrupados
                .Sum(x => x.SubTotal);

            var descuentoGeneral = Math.Round(
                subtotalProductos *
                (proforma.DescuentoPorcentaje / 100m),
                2);

            var totalPares = detallesAgrupados
                .Sum(x => x.Cantidad);

            var primaryColor = "#0f172a";
            var accentColor = "#2563eb";
            var lightGray = "#f8fafc";

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(0);

                    page.Content().Column(col =>
                    {
                        col.Item().Background(accentColor).Height(15);

                        col.Item().Padding(25).Column(content =>
                        {
                            content.Spacing(20);

                            // 🔷 HEADER
                            content.Item().Row(row =>
                            {
                                row.ConstantItem(200).Height(100).Image(logoPath);

                                row.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text("PROFORMA").FontSize(28).Bold().FontColor(primaryColor);
                                    c.Item().Text($"N° {proforma.Numero}").FontSize(25).FontColor("#6b7280");
                                    c.Item().Text(proforma.Fecha.ToLocalTime().ToString("dd/MM/yyyy"))
                                        .FontSize(12).FontColor("#6b7280");
                                });
                            });

                            // 🔷 EMPRESA / CLIENTE
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor("#e5e7eb").Padding(20).Column(c =>
                                {
                                    c.Item().Text("EMPRESA").Bold().FontSize(10).FontColor("#6b7280");
                                    c.Item().Text(proforma.Empresa.Nombre).Bold().FontSize(12);
                                    c.Item().Text($"Cédula: {proforma.Empresa.CedulaJuridica}");
                                    c.Item().Text($"Tel: {proforma.Empresa.Telefono}");
                                    c.Item().Text(proforma.Empresa.Direccion);
                                });

                                row.ConstantItem(15);

                                row.RelativeItem().Border(1).BorderColor("#e5e7eb").Padding(12).Column(c =>
                                {
                                    c.Item().Text("CLIENTE").Bold().FontSize(10).FontColor("#6b7280");

                                    c.Item().Text($"{proforma.Cliente.Nombre} {proforma.Cliente.Apellidos}")
                                        .Bold().FontSize(12);
                                    c.Item().Text($"Tel: {proforma.Cliente.Telefonos}");
                                    c.Item().Text(proforma.Cliente.Direccion);
                                    c.Item().Text($"Agente: {proforma.Cliente.Agente}");
                                });
                            });

                            // 🔷 INFO EXTRA
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Facturado por: {proforma.FacturadoPor}");
                                row.RelativeItem().Text($"Cajas: {proforma.NumeroCajas}");
                                row.RelativeItem().Text($"Transporte: {proforma.Cliente.Transporte}");
                            });

                            // 🔷 TABLA (MISMO DISEÑO, SOLO CAMBIA DATA)
                            content.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Header(header =>
                                {
                                    table.Header(header =>
                                    {
                                        header.Cell()
                                            .BorderBottom(2)
                                            .BorderColor(accentColor)
                                            .Padding(6)
                                            .Text("Código")
                                            .Bold();

                                        header.Cell()
                                            .BorderBottom(2)
                                            .BorderColor(accentColor)
                                            .Padding(6)
                                            .Text("Color")
                                            .Bold();

                                        header.Cell()
                                            .BorderBottom(2)
                                            .BorderColor(accentColor)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text("Cant")
                                            .Bold();

                                        header.Cell()
                                            .BorderBottom(2)
                                            .BorderColor(accentColor)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text("Precio")
                                            .Bold();

                                        header.Cell()
                                            .BorderBottom(2)
                                            .BorderColor(accentColor)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text("Desc.")
                                            .Bold();

                                        header.Cell()
                                            .BorderBottom(2)
                                            .BorderColor(accentColor)
                                            .Padding(6)
                                            .AlignRight()
                                            .Text("Subtotal")
                                            .Bold();
                                    });
                                });

                                foreach (var item in detallesAgrupados)
                                {
                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(6)
                                        .Text(item.Codigo);

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(6)
                                        .Text(item.Color);

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(6)
                                        .AlignRight()
                                        .Text(item.Cantidad.ToString());

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(6)
                                        .AlignRight()
                                        .Text($"₡ {item.PrecioUnitario:N2}");

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(6)
                                        .AlignRight()
                                        .Text(item.DescuentoPorcentaje > 0
                                            ? $"{item.DescuentoPorcentaje:N2}%"
                                            : "0%");

                                    table.Cell()
                                        .BorderBottom(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(6)
                                        .AlignRight()
                                        .Text($"₡ {item.SubTotal:N2}");
                                }
                            });

                            // 🔷 TOTAL
                            content.Item()
                                .AlignRight()
                                .Width(300)
                                .Column(total =>
                                {
                                    total.Spacing(5);

                                    total.Item()
                                        .BorderTop(2)
                                        .BorderColor("#e5e7eb");

                                    // TOTAL DE PARES
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text("TOTAL DE PARES")
                                            .Bold()
                                            .FontSize(11);

                                        row.RelativeItem()
                                            .AlignRight()
                                            .Text(totalPares.ToString())
                                            .Bold()
                                            .FontSize(11);
                                    });

                                    // SUBTOTAL
                                    total.Item().Row(row =>
                                    {
                                        row.RelativeItem()
                                            .Text("SUBTOTAL");

                                        row.RelativeItem()
                                            .AlignRight()
                                            .Text($"₡ {subtotalProductos:N2}");
                                    });

                                    // DESCUENTO GENERAL
                                    if (proforma.DescuentoPorcentaje > 0)
                                    {
                                        total.Item().Row(row =>
                                        {
                                            row.RelativeItem()
                                                .Text(
                                                    $"Descuento general ({proforma.DescuentoPorcentaje:N2}%)");

                                            row.RelativeItem()
                                                .AlignRight()
                                                .Text($"-₡ {descuentoGeneral:N2}");
                                        });
                                    }

                                    // TOTAL FINAL
                                    total.Item()
                                        .PaddingTop(5)
                                        .Row(row =>
                                        {
                                            row.RelativeItem()
                                            .Text("TOTAL")
                                            .Bold()
                                            .FontSize(12);

                                            row.RelativeItem()
                                            .AlignRight()
                                            .Text($"₡ {proforma.Total:N2}")
                                            .Bold()
                                            .FontSize(18)
                                            .FontColor(primaryColor);
                                        });
                                });

                            // 🔷 DETALLE
                            if (!string.IsNullOrWhiteSpace(proforma.Detalle))
                            {
                                content.Item().Column(det =>
                                {
                                    det.Item().Text("DETALLE").Bold().FontSize(11).FontColor("#6b7280");

                                    det.Item().Background("#f9fafb")
                                        .Border(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(8)
                                        .Text(proforma.Detalle)
                                        .FontSize(11);
                                });
                            }

                            // 🔷 CUENTAS
                            content.Item().Background(lightGray).Padding(10).Column(c =>
                            {
                                c.Item().Text("CUENTAS BANCARIAS").Bold().FontSize(11);

                                c.Item().Text($"BAC: {proforma.Empresa.CuentaBAC}");
                                c.Item().Text($"BCR: {proforma.Empresa.CuentaBCR}");
                                c.Item().Text($"BN: {proforma.Empresa.CuentaBN}");
                                c.Item().Text($"SINPE: {proforma.Empresa.SimpeMovil}");
                            });
                        });
                    });

                    page.Footer()
                        .Padding(10)
                        .AlignCenter()
                        .Text("Documento generado automáticamente - Sistema Empresarial")
                        .FontSize(9)
                        .FontColor("#6b7280");
                });
            }).GeneratePdf();

            return File(pdf, "application/pdf", $"Proforma_{proforma.Id}.pdf");
        }

        // ==========================
        // BUSQUEDAS
        // ==========================
        [HttpGet]
        public IActionResult BuscarClientes(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            term = term.ToLower();

            var clientes = _context.Clientes
                .AsEnumerable()
                .Where(c =>
                    c.Cedula.ToString().Contains(term) ||
                    (c.Nombre + " " + c.Apellidos).ToLower().Contains(term) ||
                    (c.Comercio ?? "").ToLower().Contains(term)
                )
                .Take(10)
                .Select(c => new
                {
                    cedula = c.Cedula,
                    nombre = c.Nombre + " " + c.Apellidos,
                    comercio = c.Comercio
                })
                .ToList();

            return Json(clientes);
        }

        [HttpGet]
        public IActionResult BuscarProductos(string term, int proformaId)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            term = term.ToLower().Trim();

            // 🔥 EMPRESA DE LA PROFORMA
            var empresaNombre = _context.Proformas
                .Where(p => p.Id == proformaId)
                .Select(p => p.Empresa!.Nombre)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(empresaNombre))
                return Json(new List<object>());

            empresaNombre = empresaNombre.ToLower().Trim();

            // 🔥 BUSCAR INVENTARIO
            var productos = _context.Inventarios
                .Where(i =>
                    i.Empresa.ToLower().Trim() == empresaNombre &&
                    (
                        i.Codigo.ToLower().Contains(term)
                        || (i.Marca != null && i.Marca.ToLower().Contains(term))
                    )
                )
                .OrderBy(i => i.Codigo)
                .Take(15)
                .Select(i => new
                {
                    codigo = i.Codigo,
                    marca = i.Marca ?? ""
                })
                .ToList();

            return Json(productos);
        }

        [HttpGet]
        public IActionResult ObtenerVariantes(string codigo, int proformaId)
        {
            var variantes = _context.TallasInventario
                .Where(t => t.InventarioCodigo == codigo)
                .OrderBy(t => t.Numero)
                .Select(t => new
                {
                    color = t.Color ?? "",
                    detalle = t.Detalle ?? "",
                    talla = t.Numero,
                    precio = t.Precio,
                    stock = t.Cantidad
                })
                .ToList();

            return Json(variantes);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerAgentes(int empresaId)
        {
            var empresa = await _context.Empresas
                .FirstOrDefaultAsync(e => e.Id == empresaId);

            if (empresa == null || string.IsNullOrWhiteSpace(empresa.Agentes))
            {
                return Json(new List<string>());
            }

            var agentes = empresa.Agentes
                .Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .ToList();

            return Json(agentes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarVenta(int id)
        {
            try
            {
                var proforma = await _context.Proformas
                    .Include(p => p.Detalles)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (proforma == null)
                {
                    TempData["Error"] =
                        "No se encontró la proforma.";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                // ========================================
                // YA EXISTE
                // ========================================

                var ventaExistente =
                    await _context.Ventas
                        .FirstOrDefaultAsync(v =>
                            v.DocumentoId == proforma.Id &&
                            v.TipoDocumento == "PROFORMA"
                        );

                if (ventaExistente != null)
                {
                    TempData["Error"] =
                        $"La venta ya existe (# {ventaExistente.Id}).";

                    return RedirectToAction(
                        nameof(Details),
                        new { id });
                }

                // ========================================
                // CANTIDAD REAL DE ZAPATOS
                // DESPUÉS DE DEVOLUCIONES
                // ========================================

                int cantidadZapatos =
                    proforma.Detalles.Sum(x =>
                        Math.Max(
                            0,
                            x.Cantidad - x.CantidadDevuelta
                        )
                    );

                // ========================================
                // FECHA UTC
                // ========================================

                var fechaVenta =
                    proforma.Fecha.Kind == DateTimeKind.Utc
                        ? proforma.Fecha
                        : DateTime.SpecifyKind(
                            proforma.Fecha,
                            DateTimeKind.Utc
                        );

                // ========================================
                // CREAR VENTA
                // ========================================

                var venta = new Venta
                {
                    Fecha = fechaVenta,

                    TipoDocumento = "PROFORMA",

                    DocumentoId = proforma.Id,

                    ClienteCedula = proforma.ClienteCedula,

                    EmpresaId = proforma.EmpresaId,

                    Total = proforma.Total,

                    NumeroCajas = proforma.NumeroCajas,

                    CantidadZapatos = cantidadZapatos,

                    FacturadoPor = proforma.FacturadoPor,

                    AgenteVenta = proforma.AgenteVenta,

                    Estado = "ACTIVA",

                    Semana =
                        ISOWeek.GetWeekOfYear(
                            fechaVenta
                        ),

                    Mes =
                        fechaVenta.Month,

                    Año =
                        fechaVenta.Year
                };

                _context.Ventas.Add(venta);

                await _context.SaveChangesAsync();

                // ========================================
                // DETALLES DE LA VENTA
                // ========================================

                foreach (var item in proforma.Detalles)
                {
                    // Cantidad que realmente queda
                    // después de las devoluciones
                    int cantidadActual =
                        Math.Max(
                            0,
                            item.Cantidad - item.CantidadDevuelta
                        );

                    // Si se devolvió todo el producto,
                    // no lo agregamos a la venta.
                    if (cantidadActual <= 0)
                        continue;

                    // ====================================
                    // SUBTOTAL REAL
                    // ====================================

                    decimal subtotalBruto =
                        cantidadActual *
                        item.PrecioUnitario;

                    // Descuento del producto
                    decimal descuentoDetalle =
                        Math.Round(
                            subtotalBruto *
                            (item.DescuentoPorcentaje / 100m),
                            2
                        );

                    decimal subtotalFinal =
                        subtotalBruto -
                        descuentoDetalle;

                    _context.VentaDetalles.Add(
                        new VentaDetalle
                        {
                            VentaId =
                                venta.Id,

                            InventarioCodigo =
                                item.InventarioCodigo,

                            Color =
                                item.Color,

                            Talla =
                                item.Talla,

                            Cantidad =
                                cantidadActual,

                            PrecioUnitario =
                                item.PrecioUnitario,

                            SubTotal =
                                subtotalFinal
                        }
                    );
                }

                await _context.SaveChangesAsync();

                TempData["Success"] =
                    $"Venta #{venta.Id} creada correctamente.";

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] =
                    ex.InnerException?.Message ??
                    ex.Message;

                return RedirectToAction(
                    nameof(Details),
                    new { id });
            }
        }
    }
}

