using ClosedXML.Excel;
using FashionM.Data;
using FashionM.Enums;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class PedidoProveedorController : Controller
    {
        private readonly AppDbContext _context;

        public PedidoProveedorController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index(int? pedidoId, string empresa, int? semana, int page = 1)
        {
            int pageSize = 25;

            var query = _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .AsQueryable();

            if (pedidoId.HasValue)
                query = query.Where(p => p.Id == pedidoId.Value);

            if (!string.IsNullOrEmpty(empresa))
                query = query.Where(p => p.Empresa == empresa);

            if (semana.HasValue)
                query = query.Where(p => p.Semana == semana.Value);

            var totalRecords = await query.CountAsync();

            var pedidos = await query
                .OrderByDescending(p => p.FechaPedido)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Empresas = new List<string>
        {
            "Cocalza Plus S.A",
            "Fashion Shoes S.A",
            "LSG Moda S.A",
            "Maxi Plus 23 S.A"
        };

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.PedidoId = pedidoId;
            ViewBag.Empresa = empresa;
            ViewBag.Semana = semana;

            return View(pedidos);
        }

        // =====================================================
        // GENERAR PEDIDOS
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> GenerarPedidos(int semana)
        {
            // =========================
            // 🔥 LIMPIAR SI YA EXISTE (REGENERAR)
            // =========================
            var existentes = await _context.PedidosProveedor
                .Where(p => p.Semana == semana)
                .ToListAsync();

            if (existentes.Any())
            {
                _context.PedidosProveedor.RemoveRange(existentes);

                var main = await _context.PedidosMain
                    .FirstOrDefaultAsync(p => p.Semana == semana);

                if (main != null)
                    _context.PedidosMain.Remove(main);

                await _context.SaveChangesAsync();
            }

            // =========================
            // 🔥 PEDIDOS VALIDOS
            // =========================
            var pedidosClientes = await _context.PedidosCliente
                .Include(p => p.Detalles)
                .Where(p =>
                    p.Semana == semana &&
                    p.EstadoCredito == EstadoCredito.Aprobado &&
                    p.AprobadoSecretaria
                )
                .ToListAsync();

            if (!pedidosClientes.Any())
            {
                TempData["Error"] = "No hay pedidos válidos para esta semana";
                return RedirectToAction("Index");
            }

            // =========================
            // 🔥 VALIDAR PROVEEDORES
            // =========================
            if (pedidosClientes
                .SelectMany(p => p.Detalles)
                .Any(d => d.ProveedorCatalogoId == null))
            {
                TempData["Error"] = "Hay productos sin proveedor asignado";
                return RedirectToAction("Index");
            }

            // =========================
            // 🔥 CREAR MAIN
            // =========================
            var pedidoMain = new PedidoMain
            {
                Semana = semana,
                FechaGenerado = DateTime.UtcNow
            };

            _context.PedidosMain.Add(pedidoMain);
            await _context.SaveChangesAsync();

            // =========================
            // 🔥 AGRUPAR DETALLES
            // =========================
            var detallesAgrupados = pedidosClientes
                .SelectMany(p => p.Detalles, (pedido, detalle) => new
                {
                    pedido.Empresa,
                    ProveedorCatalogoId = detalle.ProveedorCatalogoId.Value,
                    detalle.CodigoProducto,
                    detalle.Color,
                    detalle.Detalle,
                    detalle.Talla,
                    detalle.Cantidad
                })
                .GroupBy(x => new
                {
                    x.Empresa,
                    x.ProveedorCatalogoId,
                    x.CodigoProducto,
                    x.Color,
                    x.Detalle,
                    x.Talla
                })
                .Select(g => new
                {
                    g.Key.Empresa,
                    g.Key.ProveedorCatalogoId,
                    g.Key.CodigoProducto,
                    g.Key.Color,
                    g.Key.Detalle,
                    g.Key.Talla,
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .ToList();

            // =========================
            // 🔥 AGRUPAR POR PROVEEDOR
            // =========================
            var pedidosProveedor = detallesAgrupados
                .GroupBy(x => new
                {
                    x.Empresa,
                    x.ProveedorCatalogoId
                });

            foreach (var grupo in pedidosProveedor)
            {
                var pedidoProveedor = new PedidoProveedor
                {
                    PedidoMainId = pedidoMain.Id,
                    Empresa = grupo.Key.Empresa,
                    Semana = semana,
                    FechaPedido = DateTime.UtcNow,
                    ProveedorCatalogoId = grupo.Key.ProveedorCatalogoId,
                    Detalles = new List<PedidoProveedorDetalle>()
                };

                foreach (var item in grupo)
                {
                    pedidoProveedor.Detalles.Add(new PedidoProveedorDetalle
                    {
                        CodigoProducto = item.CodigoProducto,
                        Color = item.Color,
                        Talla = item.Talla,
                        Detalle = item.Detalle,
                        Cantidad = item.Cantidad
                    });
                }

                _context.PedidosProveedor.Add(pedidoProveedor);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Pedidos generados correctamente";

            return RedirectToAction("Index");
        }

        // =====================================================
        // DETAILS
        // =====================================================
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            var codigos = pedido.Detalles
                .Select(d => d.CodigoProducto.Trim())
                .Distinct()
                .ToList();

            // 🔥 CAMBIO CLAVE AQUÍ
            var zapatos = await _context.ZapatosProveedor
                .Where(z => codigos.Contains(z.Codigo.Trim()))
                .ToListAsync();

            ViewBag.Zapatos = zapatos;

            return View(pedido);
        }

        // =====================================================
        // DELETE
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var pedido = await _context.PedidosProveedor
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            _context.PedidosProveedor.Remove(pedido);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        // =====================================================
        // CUADRO KARLA
        // =====================================================

        public async Task<IActionResult> ResumenProveedores(int semana)
        {
            // 🔥 Traer pedidos proveedor
            var pedidos = await _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .Where(p => p.Semana == semana)
                .ToListAsync();

            if (!pedidos.Any())
                return View(new List<object>());

            // 🔥 Traer zapatos con precios
            var codigos = pedidos
                .SelectMany(p => p.Detalles)
                .Select(d => d.CodigoProducto)
                .Distinct()
                .ToList();

            var zapatos = await _context.ZapatosProveedor
                .Include(z => z.Tallas)
                .ToListAsync();

            // 🔥 Diccionario precios Colombia
            var precios = zapatos.ToDictionary(
                z => z.Codigo.Trim().ToLower(),
                z => z.Tallas.ToDictionary(
                    t => t.Numero,
                    t => t.PrecioColombia ?? 0
                )
            );

            // =========================
            // 🔥 AGRUPAR
            // =========================
            var resultado = pedidos
                .GroupBy(p => p.Empresa)
                .Select(emp => new
                {
                    Empresa = emp.Key,

                    Proveedores = emp
                        .GroupBy(p => p.ProveedorCatalogoId)
                        .Select(g =>
                        {
                            int totalPares = 0;
                            decimal totalMonto = 0;

                            foreach (var pedido in g)
                            {
                                foreach (var d in pedido.Detalles)
                                {
                                    int cantidad = d.Cantidad;
                                    int talla = int.TryParse(d.Talla, out var t) ? t : 0;

                                    decimal precio = 0;

                                    var key = d.CodigoProducto.Trim().ToLower();

                                    if (precios.ContainsKey(key) && precios[key].ContainsKey(talla))
                                        precio = precios[key][talla];

                                    totalPares += cantidad;
                                    totalMonto += cantidad * precio;
                                }
                            }

                            return new
                            {
                                Proveedor = g.First().Proveedor.Nombre,
                                Pares = totalPares,
                                Monto = totalMonto
                            };
                        })
                        .ToList()
                })
                .ToList();

            return View(resultado);
        }

        // =====================================================
        // ECXEL
        // =====================================================

        public async Task<IActionResult> ExportarExcel(int id)
        {
            var pedido = await _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            var codigos = pedido.Detalles
                .Select(d => d.CodigoProducto.Trim())
                .Distinct()
                .ToList();

            var zapatos = await _context.ZapatosProveedor
                .Include(z => z.Tallas)
                .Where(z =>
                    codigos.Contains(z.Codigo.Trim()) &&
                    z.ProveedorCatalogoId == pedido.ProveedorCatalogoId
                )
                .ToListAsync();

            // 🔥 Diccionario precios Colombia por talla
            var tallasZapato = zapatos
                .ToDictionary(
                    z => z.Codigo.Trim().ToLower(),
                    z => z.Tallas.ToDictionary(
                        t => t.Numero,
                        t => t.PrecioColombia
                    )
                );

            var tallas = Enumerable.Range(30, 15).ToList();

            var grupos = pedido.Detalles
                .GroupBy(d => new
                {
                    d.CodigoProducto,
                    d.Color,
                    d.Detalle
                })
                .OrderBy(g => g.Key.CodigoProducto)
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Pedido");

            // =========================
            // TITULO
            // =========================
            ws.Cell("A1").Value = "PEDIDO DE PRODUCCIÓN";
            ws.Range("A1:AA1").Merge();

            var titulo = ws.Range("A1:AA1");
            titulo.Style.Font.Bold = true;
            titulo.Style.Font.FontSize = 22;
            titulo.Style.Font.FontColor = XLColor.White;
            titulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#0F172A");
            titulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Row(1).Height = 35;

            // =========================
            // INFO
            // =========================
            ws.Cell("A3").Value = "Proveedor:";
            ws.Cell("B3").Value = pedido.Proveedor?.Nombre;

            ws.Cell("A4").Value = "Empresa:";
            ws.Cell("B4").Value = pedido.Empresa;

            ws.Cell("A5").Value = "Semana:";
            ws.Cell("B5").Value = pedido.Semana;

            ws.Cell("A6").Value = "Fecha:";
            ws.Cell("B6").Value = pedido.FechaPedido.ToString("dd/MM/yyyy");

            var info = ws.Range("A3:D6");
            info.Style.Fill.BackgroundColor = XLColor.FromHtml("#F1F5F9");
            info.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.Range("A3:A6").Style.Font.Bold = true;

            // =========================
            // HEADER
            // =========================
            ws.Cell("A8").Value = "Imagen";
            ws.Cell("B8").Value = "Código";
            ws.Cell("C8").Value = "Color";
            ws.Cell("D8").Value = "Detalle";

            int col = 5;

            foreach (var t in tallas)
            {
                ws.Cell(8, col).Value = t;
                col++;
            }

            ws.Cell(8, col).Value = "Total";
            ws.Cell(8, col + 1).Value = "Total $";

            var header = ws.Range(8, 1, 8, col + 1);

            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E293B");
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Row(8).Height = 25;
            ws.Column(1).Width = 20;

            int row = 9;
            int totalGeneral = 0;
            decimal totalGeneralColombia = 0;

            // =========================
            // DATOS
            // =========================
            foreach (var g in grupos)
            {
                ws.Row(row).Height = 100;

                ws.Cell(row, 2).Value = g.Key.CodigoProducto;
                ws.Cell(row, 3).Value = g.Key.Color;
                ws.Cell(row, 4).Value = g.Key.Detalle;

                int colTalla = 5;
                int totalModelo = 0;
                decimal totalColombiaModelo = 0;

                foreach (var t in tallas)
                {
                    var cantidad = g
                        .Where(x => x.Talla == t.ToString())
                        .Sum(x => x.Cantidad);

                    decimal precioCol = 0;

                    var key = g.Key.CodigoProducto.Trim().ToLower();

                    if (tallasZapato.ContainsKey(key) &&
                        tallasZapato[key].ContainsKey(t))
                    {
                        var p = tallasZapato[key][t];
                        if (p.HasValue)
                            precioCol = p.Value;
                    }

                    if (cantidad > 0)
                        ws.Cell(row, colTalla).Value = cantidad;

                    ws.Cell(row, colTalla).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    totalModelo += cantidad;
                    totalColombiaModelo += cantidad * precioCol;

                    colTalla++;
                }

                // TOTAL PARES
                ws.Cell(row, colTalla).Value = totalModelo;
                ws.Cell(row, colTalla).Style.Font.Bold = true;
                ws.Cell(row, colTalla).Style.Fill.BackgroundColor = XLColor.FromHtml("#DCFCE7");

                // TOTAL $
                ws.Cell(row, colTalla + 1).Value = totalColombiaModelo;
                ws.Cell(row, colTalla + 1).Style.NumberFormat.Format = "$ #,##0.00";
                ws.Cell(row, colTalla + 1).Style.Font.Bold = true;
                ws.Cell(row, colTalla + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF3C7");

                totalGeneral += totalModelo;
                totalGeneralColombia += totalColombiaModelo;

                // =========================
                // IMAGEN
                // =========================
                var zapato = zapatos.FirstOrDefault(z =>
                    z.Codigo.Trim().ToLower() == g.Key.CodigoProducto.Trim().ToLower()
                );

                if (zapato != null && !string.IsNullOrEmpty(zapato.ImagenUrl))
                {
                    var imgPath = zapato.ImagenUrl
                        .Replace("\\", "/")
                        .Replace("wwwroot/", "")
                        .TrimStart('/');

                    var fullPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        imgPath
                    );

                    if (System.IO.File.Exists(fullPath))
                    {
                        var pic = ws.AddPicture(fullPath);
                        pic.MoveTo(ws.Cell(row, 1), 5, 5);
                        pic.WithSize(90, 90);
                    }
                }

                // Zebra
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, colTalla + 1)
                        .Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
                }

                ws.Range(row, 1, row, colTalla + 1)
                    .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                row++;

                // =========================
                // FILA PRECIO COLOMBIA
                // =========================
                var codigoKey = g.Key.CodigoProducto.Trim().ToLower();

                if (tallasZapato.ContainsKey(codigoKey))
                {
                    ws.Row(row).Height = 30;

                    ws.Cell(row, 2).Value = "PRECIO COL";
                    ws.Cell(row, 2).Style.Font.Bold = true;
                    ws.Cell(row, 2).Style.Font.FontColor = XLColor.DarkGreen;

                    int colPrecio = 5;

                    foreach (var t in tallas)
                    {
                        if (tallasZapato[codigoKey].ContainsKey(t))
                        {
                            var precioCol = tallasZapato[codigoKey][t];

                            if (precioCol.HasValue)
                            {
                                ws.Cell(row, colPrecio).Value = precioCol.Value;
                                ws.Cell(row, colPrecio).Style.NumberFormat.Format = "$ #,##0.00";
                            }
                        }

                        ws.Cell(row, colPrecio).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        colPrecio++;
                    }

                    ws.Range(row, 1, row, colPrecio - 1)
                        .Style.Fill.BackgroundColor = XLColor.FromHtml("#ECFDF5");

                    row++;
                }
            }

            // =========================
            // TOTAL GENERAL
            // =========================
            ws.Cell(row + 1, 4).Value = "TOTAL GENERAL:";
            ws.Cell(row + 1, 4).Style.Font.Bold = true;

            ws.Cell(row + 1, 5).Value = totalGeneral;
            ws.Cell(row + 1, 5).Style.Font.Bold = true;

            ws.Cell(row + 1, 6).Value = totalGeneralColombia;
            ws.Cell(row + 1, 6).Style.NumberFormat.Format = "$ #,##0.00";
            ws.Cell(row + 1, 6).Style.Font.Bold = true;

            ws.Range(row + 1, 4, row + 1, 6)
                .Style.Fill.BackgroundColor = XLColor.FromHtml("#FEF9C3");

            // =========================
            // FINAL
            // =========================
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(8);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"PedidoProveedor_{pedido.Id}.xlsx"
            );
        }
    }
}
