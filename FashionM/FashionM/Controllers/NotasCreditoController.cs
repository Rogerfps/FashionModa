using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class NotasCreditoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public NotasCreditoController(
    AppDbContext context,
    IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ========================================
        // INDEX
        // ========================================

        public async Task<IActionResult> Index()
        {
            var notas = await _context.NotasCredito
                .Include(n => n.Cliente)
                .Include(n => n.Empresa)
                .Include(n => n.Venta)
                .OrderByDescending(n => n.Fecha)
                .ToListAsync();

            return View(notas);
        }


        /// ========================================
        // DETAILS
        // ========================================

        public async Task<IActionResult> Details(int id)
        {
            var nota = await _context.NotasCredito
                .Include(n => n.Cliente)

                .Include(n => n.Empresa)

                .Include(n => n.Venta)

                .ThenInclude(v => v!.Detalles)

                .Include(n => n.Detalles)

                .ThenInclude(d => d.VentaDetalle)

                .FirstOrDefaultAsync(n => n.Id == id);

            if (nota == null)
                return NotFound();

            return View(nota);
        }


        // ========================================
        // CREATE
        // ========================================

        public async Task<IActionResult> Create(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            if (venta == null)
                return NotFound();

            foreach (var detalle in venta.Detalles)
            {
                int yaDevuelto =
                    await _context.NotaCreditoDetalles
                        .Where(n =>
                            n.VentaDetalleId == detalle.Id)
                        .SumAsync(x =>
                            x.CantidadDevuelta);

                ViewData[$"Disponible_{detalle.Id}"] =
                    detalle.Cantidad - yaDevuelto;
            }

            return View(venta);
        }

        // ========================================
        // CREATE POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int ventaId,
            string motivo,
            decimal descuentoGlobal,
            List<int> detalleIds,
            List<int> cantidadesDevueltas,
            List<decimal> preciosCorregidos,
            List<decimal> descuentosLinea,
            List<int> eliminarLineaIds,
            List<string> observaciones)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            if (venta == null)
                return NotFound();

            var nota = new NotaCredito
            {
                VentaId = venta.Id,

                Fecha = DateTime.UtcNow,

                Motivo = motivo,

                TipoDocumento =
                    venta.TipoDocumento,

                ClienteCedula =
                    venta.ClienteCedula,

                EmpresaId =
                    venta.EmpresaId,

                Semana =
                    ISOWeek.GetWeekOfYear(
                        DateTime.UtcNow),

                Mes =
                    DateTime.UtcNow.Month,

                Año =
                    DateTime.UtcNow.Year,

                AgenteVenta =
                    venta.AgenteVenta,

                Estado = "ACTIVA",

                DescuentoGlobal =
                    descuentoGlobal
            };

            // ========================================
            // TOTALES
            // ========================================

            decimal subtotalGeneral = 0;

            decimal totalDevueltoReal = 0;

            // ========================================
            // RECORRER PRODUCTOS
            // ========================================

            for (int i = 0; i < detalleIds.Count; i++)
            {
                int detalleId =
                    detalleIds[i];

                int cantidadDevuelta =
                    cantidadesDevueltas.Count > i
                        ? cantidadesDevueltas[i]
                        : 0;

                decimal precioCorregidoInput =
                    preciosCorregidos.Count > i
                        ? preciosCorregidos[i]
                        : 0;

                decimal descuentoLinea =
                    descuentosLinea.Count > i
                        ? descuentosLinea[i]
                        : 0;

                bool eliminado =
                    eliminarLineaIds != null &&
                    eliminarLineaIds.Contains(detalleId);

                string observacion =
                    observaciones.Count > i
                        ? observaciones[i]
                        : string.Empty;

                var detalleVenta =
                    venta.Detalles
                        .FirstOrDefault(x =>
                            x.Id == detalleId);

                if (detalleVenta == null)
                    continue;

                // ========================================
                // VALIDAR DEVOLUCIONES PREVIAS
                // ========================================

                int yaDevuelto =
                    await _context.NotaCreditoDetalles
                        .Where(x =>
                            x.VentaDetalleId ==
                            detalleVenta.Id)
                        .SumAsync(x =>
                            x.CantidadDevuelta);

                int disponible =
                    detalleVenta.Cantidad -
                    yaDevuelto;

                if (disponible <= 0)
                    continue;

                // ========================================
                // ELIMINAR LÍNEA
                // ========================================

                if (eliminado)
                {
                    cantidadDevuelta =
                        disponible;
                }

                // ========================================
                // VALIDAR DISPONIBLE
                // ========================================

                if (cantidadDevuelta > disponible)
                {
                    TempData["Error"] =
                        $"La cantidad máxima disponible para devolver del producto {detalleVenta.InventarioCodigo} es {disponible}.";

                    return RedirectToAction(
                        nameof(Create),
                        new { ventaId });
                }

                // ========================================
                // PRECIO FINAL
                // ========================================

                decimal precioFinal =
                    precioCorregidoInput > 0
                        ? precioCorregidoInput
                        : detalleVenta.PrecioUnitario;

                // ========================================
                // VALIDAR PRECIO
                // ========================================

                if (precioFinal >
                    detalleVenta.PrecioUnitario)
                {
                    TempData["Error"] =
                        $"El precio corregido no puede ser mayor al original para el producto {detalleVenta.InventarioCodigo}.";

                    return RedirectToAction(
                        nameof(Create),
                        new { ventaId });
                }

                // ========================================
                // VALIDAR CAMBIOS
                // ========================================

                bool tieneCambios =
                    cantidadDevuelta > 0 ||
                    descuentoLinea > 0 ||
                    precioFinal != detalleVenta.PrecioUnitario ||
                    eliminado;

                if (!tieneCambios)
                    continue;

                // ========================================
                // CANTIDAD AFECTADA
                // ========================================

                int cantidadAfectada = 0;

                // DEVOLUCIÓN
                if (cantidadDevuelta > 0)
                {
                    cantidadAfectada =
                        cantidadDevuelta;
                }

                // DESCUENTO / PRECIO CORREGIDO
                else if (
                    descuentoLinea > 0 ||
                    precioFinal != detalleVenta.PrecioUnitario
                )
                {
                    cantidadAfectada =
                        disponible;
                }

                // ELIMINACIÓN
                if (eliminado)
                {
                    cantidadAfectada =
                        disponible;
                }

                // ========================================
                // TOTAL ORIGINAL
                // ========================================

                decimal totalOriginal =
                    cantidadAfectada *
                    detalleVenta.PrecioUnitario;

                // ========================================
                // NUEVO SUBTOTAL
                // ========================================

                decimal subtotal =
                    cantidadAfectada *
                    precioFinal;

                // ========================================
                // DESCUENTO LÍNEA
                // ========================================

                if (descuentoLinea > 0)
                {
                    subtotal -=
                        subtotal *
                        (descuentoLinea / 100);
                }

                // ========================================
                // DEVOLUCIÓN REAL
                // ========================================

                decimal devolucionLinea = 0;

                // DEVOLUCIÓN DE PRODUCTOS
                if (
                    cantidadDevuelta > 0 &&
                    descuentoLinea <= 0 &&
                    precioFinal == detalleVenta.PrecioUnitario
                )
                {
                    devolucionLinea =
                        subtotal;
                }

                // AJUSTE COMERCIAL
                else
                {
                    devolucionLinea =
                        totalOriginal - subtotal;
                }

                subtotalGeneral += subtotal;

                totalDevueltoReal +=
                    devolucionLinea;

                // ========================================
                // GUARDAR DETALLE
                // ========================================

                nota.Detalles.Add(
                    new NotaCreditoDetalle
                    {
                        VentaDetalleId =
                            detalleVenta.Id,

                        InventarioCodigo =
                            detalleVenta.InventarioCodigo,

                        Color =
                            detalleVenta.Color,

                        Talla =
                            detalleVenta.Talla,

                        CantidadOriginal =
                            detalleVenta.Cantidad,

                        CantidadDevuelta =
                            cantidadDevuelta,

                        PrecioOriginal =
                            detalleVenta.PrecioUnitario,

                        PrecioCorregido =
                            precioFinal,

                        DescuentoLinea =
                            descuentoLinea,

                        Eliminado =
                            eliminado,

                        Observaciones =
                            observacion ?? string.Empty,

                        SubTotal =
                            subtotal
                    });
            }

            // ========================================
            // SOLO DESCUENTO GLOBAL
            // ========================================

            bool soloDescuentoGlobal =
                descuentoGlobal > 0 &&
                !nota.Detalles.Any();

            if (soloDescuentoGlobal)
            {
                subtotalGeneral =
                    venta.Total;

                totalDevueltoReal =
                    venta.Total *
                    (descuentoGlobal / 100);
            }

            // ========================================
            // VALIDAR
            // ========================================

            if (!nota.Detalles.Any() &&
                !soloDescuentoGlobal)
            {
                TempData["Error"] =
                    "La nota crédito no contiene ajustes válidos.";

                return RedirectToAction(
                    nameof(Create),
                    new { ventaId });
            }

            // ========================================
            // SUBTOTAL
            // ========================================

            nota.SubTotal =
                subtotalGeneral;

            // ========================================
            // DESCUENTO GLOBAL
            // ========================================

            decimal devolucionGlobal = 0;

            if (descuentoGlobal > 0)
            {
                devolucionGlobal =
                    subtotalGeneral *
                    (descuentoGlobal / 100);
            }

            // ========================================
            // TOTAL DEVUELTO
            // ========================================

            nota.TotalDevuelto =
                totalDevueltoReal +
                devolucionGlobal;

            // ========================================
            // GUARDAR
            // ========================================

            _context.NotasCredito.Add(nota);

            // ========================================
            // VALIDAR ESTADO VENTA
            // ========================================

            bool ventaAnulada = true;

            foreach (var detalle in venta.Detalles)
            {
                int totalDevuelto =
                    await _context.NotaCreditoDetalles
                        .Where(x =>
                            x.VentaDetalleId ==
                            detalle.Id)
                        .SumAsync(x =>
                            x.CantidadDevuelta);

                totalDevuelto +=
                    nota.Detalles
                        .Where(x =>
                            x.VentaDetalleId ==
                            detalle.Id)
                        .Sum(x =>
                            x.CantidadDevuelta);

                if (totalDevuelto <
                    detalle.Cantidad)
                {
                    ventaAnulada = false;
                    break;
                }
            }

            if (ventaAnulada)
            {
                venta.Estado =
                    "ANULADA";
            }
            else if (nota.Detalles.Any(x =>
                x.CantidadDevuelta > 0))
            {
                venta.Estado =
                    "DEVUELTA_PARCIAL";
            }
            else
            {
                venta.Estado =
                    "ACTIVA";
            }

            // ========================================
            // SAVE
            // ========================================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Nota de crédito generada correctamente.";

            return RedirectToAction(
                "Details",
                "Ventas",
                new { id = venta.Id });
        }

        //========================================
        //PDF
        //========================================

        public async Task<IActionResult> GenerarPDF(int id)
        {
            var nota = await _context.NotasCredito
                .Include(n => n.Empresa)
                .Include(n => n.Cliente)
                .Include(n => n.Venta)
                .Include(n => n.Detalles)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (nota == null)
                return NotFound();

            // ========================================
            // NORMALIZAR EMPRESA
            // ========================================

            string nombreEmpresa =
                nota.Empresa?.Nombre
                    .ToLower()
                    .Trim() ?? "";

            // ========================================
            // LOGO
            // ========================================

            string logo = nombreEmpresa switch
            {
                "cocalza plus s.a" => "cocalza.png",
                "fashion shoes s.a" => "fashion.png",
                "lsg moda s.a" => "lsg.jpg",
                "maxi plus 23 s.a" => "maxiplus.png",
                "kyroz" => "KYROZ.png",
                _ => "default.png"
            };

            var logoPath =
                Path.Combine(
                    _env.WebRootPath,
                    "images",
                    logo);

            if (!System.IO.File.Exists(logoPath))
            {
                logoPath =
                    Path.Combine(
                        _env.WebRootPath,
                        "images",
                        "default.png");
            }

            // ========================================
            // COLORES
            // ========================================

            var primaryColor = "#7f1d1d";
            var accentColor = "#dc2626";
            var lightGray = "#f8fafc";

            // ========================================
            // PDF
            // ========================================

            var pdf = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(0);

                    page.Content().Column(col =>
                    {
                        // BARRA SUPERIOR
                        col.Item()
                            .Background(accentColor)
                            .Height(15);

                        col.Item()
                            .Padding(25)
                            .Column(content =>
                            {
                                content.Spacing(20);

                                // ========================================
                                // HEADER
                                // ========================================

                                content.Item().Row(row =>
                                {
                                    row.ConstantItem(200)
                                        .Height(90)
                                        .Image(logoPath);

                                    row.RelativeItem()
                                        .AlignRight()
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("NOTA CRÉDITO")
                                                .FontSize(28)
                                                .Bold()
                                                .FontColor(primaryColor);

                                            c.Item()
                                                .Text($"N° {nota.Id}")
                                                .FontSize(24)
                                                .FontColor("#6b7280");

                                            c.Item()
                                                .Text(
                                                    nota.Fecha
                                                        .ToLocalTime()
                                                        .ToString("dd/MM/yyyy HH:mm"))
                                                .FontSize(11)
                                                .FontColor("#6b7280");
                                        });
                                });

                                // ========================================
                                // EMPRESA / CLIENTE
                                // ========================================

                                content.Item().Row(row =>
                                {
                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(15)
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("EMPRESA")
                                                .Bold()
                                                .FontSize(10)
                                                .FontColor("#6b7280");

                                            c.Item()
                                                .Text(nota.Empresa?.Nombre ?? "")
                                                .Bold()
                                                .FontSize(12);

                                            c.Item()
                                                .Text($"Cédula: {nota.Empresa?.CedulaJuridica}");

                                            c.Item()
                                                .Text($"Tel: {nota.Empresa?.Telefono}");

                                            c.Item()
                                                .Text(nota.Empresa?.Direccion ?? "");
                                        });

                                    row.ConstantItem(15);

                                    row.RelativeItem()
                                        .Border(1)
                                        .BorderColor("#e5e7eb")
                                        .Padding(15)
                                        .Column(c =>
                                        {
                                            c.Item()
                                                .Text("CLIENTE")
                                                .Bold()
                                                .FontSize(10)
                                                .FontColor("#6b7280");

                                            c.Item()
                                                .Text($"{nota.Cliente?.Nombre} {nota.Cliente?.Apellidos}")
                                                .Bold()
                                                .FontSize(12);

                                            c.Item()
                                                .Text($"Tel: {nota.Cliente?.Telefonos}");

                                            c.Item()
                                                .Text(nota.Cliente?.Direccion ?? "");

                                            c.Item()
                                                .Text($"Agente: {nota.AgenteVenta}");
                                        });
                                });

                                // ========================================
                                // INFO EXTRA
                                // ========================================

                                content.Item().Row(row =>
                                {
                                    row.RelativeItem()
                                        .Text($"Documento: {nota.TipoDocumento}");

                                    row.RelativeItem()
                                        .Text($"Venta Original: #{nota.VentaId}");

                                    row.RelativeItem()
                                        .Text($"Estado: {nota.Estado}");
                                });

                                // ========================================
                                // MOTIVO
                                // ========================================

                                content.Item()
                                    .Column(det =>
                                    {
                                        det.Item()
                                            .Text("MOTIVO")
                                            .Bold()
                                            .FontSize(11)
                                            .FontColor("#6b7280");

                                        det.Item()
                                            .Background("#f9fafb")
                                            .Border(1)
                                            .BorderColor("#e5e7eb")
                                            .Padding(10)
                                            .Text(nota.Motivo)
                                            .FontSize(11);
                                    });

                                // ========================================
                                // TABLA
                                // ========================================

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
                                        columns.RelativeColumn();
                                    });

                                    // HEADER
                                    table.Header(header =>
                                    {
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Código").Bold();
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).Text("Color").Bold();
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Cant").Bold();
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Precio").Bold();
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Desc").Bold();
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Estado").Bold();
                                        header.Cell().BorderBottom(2).BorderColor(accentColor).Padding(6).AlignRight().Text("Subtotal").Bold();
                                    });

                                    foreach (var item in nota.Detalles)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .Text(item.InventarioCodigo);

                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .Text(item.Color);

                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .AlignRight()
                                            .Text(item.CantidadDevuelta.ToString());

                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .AlignRight()
                                            .Text($"₡ {item.PrecioCorregido:N2}");

                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .AlignRight()
                                            .Text($"{item.DescuentoLinea:N2}%");

                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .AlignRight()
                                            .Text(item.Eliminado ? "ELIMINADO" : "ACTIVO");

                                        table.Cell().BorderBottom(1).BorderColor("#e5e7eb").Padding(6)
                                            .AlignRight()
                                            .Text($"₡ {item.SubTotal:N2}");
                                    }
                                });

                                // ========================================
                                // TOTALES
                                // ========================================

                                content.Item()
                                    .AlignRight()
                                    .Width(280)
                                    .Column(total =>
                                    {
                                        total.Item()
                                            .BorderTop(2)
                                            .BorderColor("#e5e7eb");

                                        total.Item()
                                            .Row(row =>
                                            {
                                                row.RelativeItem()
                                                    .Text("SUBTOTAL")
                                                    .Bold();

                                                row.RelativeItem()
                                                    .AlignRight()
                                                    .Text($"₡ {nota.SubTotal:N2}");
                                            });

                                        total.Item()
                                            .Row(row =>
                                            {
                                                row.RelativeItem()
                                                    .Text("DESC. GLOBAL")
                                                    .Bold();

                                                row.RelativeItem()
                                                    .AlignRight()
                                                    .Text($"{nota.DescuentoGlobal:N2}%");
                                            });

                                        total.Item()
                                            .PaddingTop(8)
                                            .Row(row =>
                                            {
                                                row.RelativeItem()
                                                    .Text("TOTAL DEVUELTO")
                                                    .Bold()
                                                    .FontSize(14);

                                                row.RelativeItem()
                                                    .AlignRight()
                                                    .Text($"₡ {nota.TotalDevuelto:N2}")
                                                    .Bold()
                                                    .FontSize(18)
                                                    .FontColor(primaryColor);
                                            });
                                    });

                                // ========================================
                                // CUENTAS
                                // ========================================

                                content.Item()
                                    .Background(lightGray)
                                    .Padding(10)
                                    .Column(c =>
                                    {
                                        c.Item()
                                            .Text("CUENTAS BANCARIAS")
                                            .Bold()
                                            .FontSize(11);

                                        c.Item()
                                            .Text($"BAC: {nota.Empresa?.CuentaBAC}");

                                        c.Item()
                                            .Text($"BCR: {nota.Empresa?.CuentaBCR}");

                                        c.Item()
                                            .Text($"BN: {nota.Empresa?.CuentaBN}");

                                        c.Item()
                                            .Text($"SINPE: {nota.Empresa?.SimpeMovil}");
                                    });
                            });
                    });

                    // ========================================
                    // FOOTER
                    // ========================================

                    page.Footer()
                        .Padding(10)
                        .AlignCenter()
                        .Text("Documento generado automáticamente - Sistema Empresarial")
                        .FontSize(9)
                        .FontColor("#6b7280");
                });
            }).GeneratePdf();

            return File(
                pdf,
                "application/pdf",
                $"NotaCredito_{nota.Id}.pdf");
        }

    }
}



