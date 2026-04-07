using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
            var query = _context.Proformas
                .Include(p => p.Empresa)
                .Include(p => p.Cliente)
                .AsQueryable();

            if (!string.IsNullOrEmpty(buscar))
            {
                buscar = buscar.ToLower();

                query = query.Where(p =>
                    p.Numero.ToString().Contains(buscar) // 🔥 CAMBIO
                    || p.Cliente.Nombre.ToLower().Contains(buscar)
                    || p.Cliente.Apellidos.ToLower().Contains(buscar)
                    || p.Cliente.Codigo.ToLower().Contains(buscar)
                );
            }

            if (empresaId.HasValue)
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

            // 🔥 NUMERO POR EMPRESA
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
        public async Task<IActionResult> AgregarProducto(
            int proformaId,
            string codigo,
            string color,
            string detalle,
            string[] tallas,
            int[] cantidades)
        {
            // 🔥 VALIDACIONES BÁSICAS
            if (string.IsNullOrWhiteSpace(codigo) ||
                string.IsNullOrWhiteSpace(color))
                
            {
                TempData["Error"] = "Debe completar todos los campos del producto";
                return RedirectToAction("AgregarProducto", new { id = proformaId });
            }

            if (tallas == null || cantidades == null || tallas.Length == 0)
            {
                TempData["Error"] = "Debe ingresar al menos una talla con cantidad";
                return RedirectToAction("AgregarProducto", new { id = proformaId });
            }

            var inventario = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.Codigo == codigo);

            if (inventario == null)
                return NotFound();

            bool agregoAlgo = false;

            // 🔥 LOOP SEGURO
            for (int i = 0; i < tallas.Length; i++)
            {
                // evitar desbordes
                if (i >= cantidades.Length)
                    break;

                int cantidad = cantidades[i];

                if (cantidad <= 0)
                    continue;

                var tallaInventario = await _context.TallasInventario
                    .FirstOrDefaultAsync(t =>
                        t.InventarioCodigo == codigo &&
                        t.Color == color &&
                        (t.Detalle ?? "").Trim() == (detalle ?? "").Trim() &&
                        (t.Numero ?? "").Trim() == (tallas[i] ?? "").Trim());

                if (tallaInventario == null)
                    continue;

                if (tallaInventario.Cantidad < cantidad)
                    continue;

                decimal precio = tallaInventario.Precio > 0
                    ? tallaInventario.Precio
                    : inventario.PrecioVenta;

                var detalleProforma = new ProformaDetalle
                {
                    ProformaId = proformaId,
                    InventarioCodigo = codigo,
                    Color = color,
                    Talla = tallas[i],
                    Cantidad = cantidad,
                    PrecioUnitario = precio,
                    SubTotal = precio * cantidad
                };

                // 🔽 DESCONTAR STOCK
                tallaInventario.Cantidad -= cantidad;

                _context.ProformaDetalles.Add(detalleProforma);

                agregoAlgo = true;
            }

            // 🔥 VALIDAR SI NO AGREGÓ NADA
            if (!agregoAlgo)
            {
                TempData["Error"] = "No se agregaron productos. Verifique cantidades o stock.";
                return RedirectToAction("AgregarProducto", new { id = proformaId });
            }

            await _context.SaveChangesAsync();

            await ActualizarTotal(proformaId);

            return RedirectToAction("AgregarProducto", new { id = proformaId });
        }

        // ==========================
        // ACTUALIZAR TOTAL
        // ==========================
        private async Task ActualizarTotal(int proformaId)
        {
            var proforma = await _context.Proformas
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == proformaId);

            if (proforma == null)
                return;

            proforma.Total = proforma.Detalles.Sum(d => d.SubTotal);

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
                    d.PrecioUnitario
                })
                .Select(g => new
                {
                    Codigo = g.Key.InventarioCodigo,
                    Color = g.Key.Color,
                    Cantidad = g.Sum(x => x.Cantidad),
                    PrecioUnitario = g.Key.PrecioUnitario,
                    SubTotal = g.Sum(x => x.SubTotal)
                })
                .ToList();

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
                                });

                                table.Header(header =>
                                {
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Código").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Color").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Cant").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Precio").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Subtotal").Bold();
                                });

                                foreach (var item in detallesAgrupados)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).Text(item.Codigo);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).Text(item.Color);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).AlignRight().Text(item.Cantidad.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).AlignRight().Text($"₡ {item.PrecioUnitario:N2}");
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).AlignRight().Text($"₡ {item.SubTotal:N2}");
                                }
                            });

                            // 🔷 TOTAL
                            content.Item().AlignRight().Width(250).Column(total =>
                            {
                                total.Item().BorderTop(2).BorderColor("#e5e7eb");

                                total.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("TOTAL").Bold().FontSize(12);

                                    row.RelativeItem().AlignRight().Text($"₡ {proforma.Total:N2}")
                                        .Bold().FontSize(18).FontColor(primaryColor);
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

            // 🔥 Obtener la empresa de la proforma
            var empresaNombre = _context.Proformas
                .Where(p => p.Id == proformaId)
                .Select(p => p.Empresa.Nombre)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(empresaNombre))
                return Json(new List<object>());

            var productos = _context.Inventarios
                .Where(i =>
                    i.Empresa.ToLower().Trim() == empresaNombre.ToLower().Trim() && // 🔥 filtro correcto
                    (i.Codigo.Contains(term) || i.Marca.Contains(term))
                )
                .Take(10)
                .Select(i => new
                {
                    codigo = i.Codigo,
                    marca = i.Marca
                })
                .ToList();

            return Json(productos);
        }

        [HttpGet]
        public IActionResult ObtenerVariantes(string codigo, int proformaId)
        {
            var variantes = _context.TallasInventario
                .Where(t => t.InventarioCodigo == codigo)
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
    }
}