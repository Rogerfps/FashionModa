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

            // 🔍 FILTRO POR NUMERO
            if (!string.IsNullOrEmpty(buscar))
            {
                buscar = buscar.ToLower();

                query = query.Where(p =>
                    // 🔢 Buscar por número de proforma
                    p.Id.ToString().Contains(buscar)

                    // 👤 Nombre cliente
                    || p.Cliente.Nombre.ToLower().Contains(buscar)

                    // 👤 Apellidos
                    || p.Cliente.Apellidos.ToLower().Contains(buscar)

                    // 🆔 Código cliente
                    || p.Cliente.Codigo.ToLower().Contains(buscar)
                );
            }

            // 🏢 FILTRO POR EMPRESA
            if (empresaId.HasValue)
            {
                query = query.Where(p => p.EmpresaId == empresaId.Value);
            }

            var proformas = await query
                .OrderByDescending(p => p.Fecha)
                .ToListAsync();

            // 🔽 PARA EL DROPDOWN
            ViewBag.Empresas = await _context.Empresas.ToListAsync();

            // 🔽 MANTENER VALORES
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
            ViewBag.Clientes = new SelectList(_context.Clientes, "Cedula", "Nombre");

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
                ViewBag.Clientes = new SelectList(_context.Clientes, "Cedula", "Nombre", proforma.ClienteCedula);

                return View(proforma);
            }

            proforma.Fecha = DateTime.UtcNow;

            _context.Add(proforma);
            await _context.SaveChangesAsync();

            return RedirectToAction("AgregarProducto", new { id = proforma.Id });
        }

        // ==========================
        // AGREGAR PRODUCTOS
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

            ViewBag.Productos = await _context.Inventarios
                .OrderBy(i => i.Codigo)
                .ToListAsync();

            return View(proforma);
        }

        // ==========================
        // AGREGAR PRODUCTO POST
        // ==========================
        [HttpPost]
        public async Task<IActionResult> AgregarProducto(
            int proformaId,
            string codigo,
            string color,
            string talla,
            int cantidad)
        {
            var inventario = await _context.Inventarios
                .FirstOrDefaultAsync(i => i.Codigo == codigo);

            if (inventario == null)
                return NotFound();

            var tallaInventario = await _context.TallasInventario
                .FirstOrDefaultAsync(t =>
                    t.InventarioCodigo == codigo &&
                    t.Color == color &&
                    t.Numero == talla);

            if (tallaInventario == null)
            {
                TempData["Error"] = "Talla no encontrada";
                return RedirectToAction("AgregarProducto", new { id = proformaId });
            }

            if (tallaInventario.Cantidad < cantidad)
            {
                TempData["Error"] = "Stock insuficiente";
                return RedirectToAction("AgregarProducto", new { id = proformaId });
            }

            decimal precio = tallaInventario.Precio > 0
                ? tallaInventario.Precio
                : inventario.PrecioVenta;

            var detalle = new ProformaDetalle
            {
                ProformaId = proformaId,
                InventarioCodigo = codigo,
                Color = color,
                Talla = talla,
                Cantidad = cantidad,
                PrecioUnitario = precio,
                SubTotal = precio * cantidad
            };

            // rebajar inventario
            tallaInventario.Cantidad -= cantidad;

            _context.ProformaDetalles.Add(detalle);

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
        // ELIMINAR
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
                "cocalza plus" => "cocalza.png",
                "fashion shoes s.a" => "fashion.png",
                "lsg moda s.a" => "lsg.jpg",
                "maxiplus" => "maxiplus.png",
                _ => "default.png"
            };

            // 🔽 RUTA DEL LOGO
            var logoPath = Path.Combine(
                _env.WebRootPath,
                "images",
                logo
            );

            // Validar si existe
            if (!System.IO.File.Exists(logoPath))
            {
                logoPath = Path.Combine(
                    _env.WebRootPath,
                    "images",
                    "default.png"
                );
            }

            var primaryColor = "#0f172a";   // oscuro elegante
            var accentColor = "#2563eb";    // azul moderno
            var lightGray = "#f8fafc";

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(0);

                    page.Content().Column(col =>
                    {
                        // 🔷 BARRA SUPERIOR (branding)
                        col.Item().Background(accentColor).Height(15);

                        // 🔷 CONTENIDO PRINCIPAL
                        col.Item().Padding(25).Column(content =>
                        {
                            content.Spacing(20);

                            // 🔷 HEADER
                            content.Item().Row(row =>
                            {
                                row.ConstantItem(200).Height(100).Image(logoPath);

                                row.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text("PROFORMA")
                                        .FontSize(28)
                                        .Bold()
                                        .FontColor(primaryColor);

                                    c.Item().Text($"N° {proforma.Id}")
                                        .FontSize(25)
                                        .FontColor("#6b7280");

                                    c.Item().Text(proforma.Fecha.ToLocalTime().ToString("dd/MM/yyyy"))
                                        .FontSize(12)
                                        .FontColor("#6b7280");
                                });
                            });

                            // 🔷 BLOQUES (EMPRESA / CLIENTE)
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Border(1).BorderColor("#e5e7eb").Padding(20).Column(c =>
                                {
                                    c.Item().Text("EMPRESA")
                                        .Bold()
                                        .FontSize(10)
                                        .FontColor("#6b7280");

                                    c.Item().Text(proforma.Empresa.Nombre).Bold().FontSize(12);
                                    c.Item().Text($"Cédula: {proforma.Empresa.CedulaJuridica}");
                                    c.Item().Text($"Tel: {proforma.Empresa.Telefono}");
                                    c.Item().Text(proforma.Empresa.Direccion);
                                });

                                row.ConstantItem(15);

                                row.RelativeItem().Border(1).BorderColor("#e5e7eb").Padding(12).Column(c =>
                                {
                                    c.Item().Text("CLIENTE")
                                        .Bold()
                                        .FontSize(10)
                                        .FontColor("#6b7280");

                                    c.Item().Text($"{proforma.Cliente.Nombre} {proforma.Cliente.Apellidos}")
                                        .Bold().FontSize(12);

                                    //c.Item().Text($"Código: {proforma.Cliente.Codigo}");
                                    c.Item().Text($"Tel: {proforma.Cliente.Telefonos}");
                                    c.Item().Text(proforma.Cliente.Direccion);
                                    c.Item().Text($"Agente: {proforma.Cliente.Agente}");
                                });
                            });

                            // 🔷 INFO EXTRA EN LÍNEA
                            content.Item().Row(row =>
                            {
                                row.RelativeItem().Text($"Facturado por: {proforma.FacturadoPor}");
                                row.RelativeItem().Text($"Cajas: {proforma.NumeroCajas}");
                                row.RelativeItem().Text($"Transporte: {proforma.Cliente.Transporte}");
                            });

                            // 🔷 TABLA
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

                                // HEADER
                                table.Header(header =>
                                {
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Código").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Color").Bold();
                                    //header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Talla").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Cant").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Precio").Bold();
                                    header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Subtotal").Bold();
                                });

                                foreach (var item in proforma.Detalles)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).Text(item.InventarioCodigo);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).Text(item.Color);
                                    //table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).Text(item.Talla);
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).AlignRight().Text(item.Cantidad.ToString());
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).AlignRight().Text($"₡ {item.PrecioUnitario:N2}");
                                    table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6).AlignRight().Text($"₡ {item.SubTotal:N2}");
                                }
                            });

                            // 🔷 TOTAL PROFESIONAL (tipo factura real)
                            content.Item().AlignRight().Width(250).Column(total =>
                            {
                                total.Item().BorderTop(2).BorderColor("#e5e7eb");

                                total.Item().Row(row =>
                                {
                                    row.RelativeItem().Text("TOTAL")
                                        .Bold()
                                        .FontSize(12);

                                    row.RelativeItem().AlignRight().Text($"₡ {proforma.Total:N2}")
                                        .Bold()
                                        .FontSize(18)
                                        .FontColor(primaryColor);
                                });
                            });

                            if (!string.IsNullOrWhiteSpace(proforma.Detalle))
                            {
                                content.Item().Column(det =>
                                {
                                    det.Item().Text("DETALLE")
                                        .Bold()
                                        .FontSize(11)
                                        .FontColor("#6b7280");

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
                                c.Item().Text("CUENTAS BANCARIAS")
                                    .Bold()
                                    .FontSize(11);

                                c.Item().Text($"BAC: {proforma.Empresa.CuentaBAC}");
                                c.Item().Text($"BCR: {proforma.Empresa.CuentaBCR}");
                                c.Item().Text($"BN: {proforma.Empresa.CuentaBN}");
                                c.Item().Text($"SINPE: {proforma.Empresa.SimpeMovil}");
                            });
                        });
                    });

                    // 🔷 FOOTER SERIO
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

        [HttpGet]
        public IActionResult BuscarClientes(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            term = term.ToLower();

            var clientes = _context.Clientes
                .AsEnumerable() // 🔥 evita problemas de EF
                .Where(c =>
                    c.Cedula.ToString().Contains(term) ||
                    (c.Nombre + " " + c.Apellidos).ToLower().Contains(term) ||
                    (c.Comercio ?? "").ToLower().Contains(term)
                )
                .OrderBy(c => c.Nombre)
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
        public IActionResult BuscarProductos(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>());

            var productos = _context.Inventarios
                .Where(i =>
                    i.Codigo.Contains(term) ||
                    i.Marca.Contains(term)
                )
                .OrderBy(i => i.Codigo)
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
        public IActionResult ObtenerVariantes(string codigo)
        {
            var variantes = _context.TallasInventario
                .Where(t => t.InventarioCodigo == codigo)
                .Select(t => new
                {
                    color = t.Color,
                    detalle = t.Detalle,
                    talla = t.Numero,
                    precio = t.Precio
                })
                .ToList();

            return Json(variantes);
        }
    }
}