using ClosedXML.Excel;
using FashionM.Data;
using FashionM.Enums;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FashionM.Controllers
{
    public class PedidoProveedorController : Controller
    {
        private readonly AppDbContext _context;

        public PedidoProveedorController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTA DE PEDIDOS GENERADOS
        // =====================================================
        public async Task<IActionResult> Index(
            int? pedidoId,
            string empresa,
            int? semana,
            int page = 1)
        {
            int pageSize = 25;

            var query = _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .AsQueryable();

            // =========================
            // FILTRO POR ID PEDIDO
            // =========================
            if (pedidoId.HasValue)
            {
                query = query.Where(p => p.Id == pedidoId.Value);
            }

            // =========================
            // FILTRO POR EMPRESA
            // =========================
            if (!string.IsNullOrEmpty(empresa))
            {
                query = query.Where(p => p.Empresa == empresa);
            }

            // =========================
            // FILTRO POR SEMANA
            // =========================
            if (semana.HasValue)
            {
                query = query.Where(p => p.Semana == semana.Value);
            }

            // =========================
            // TOTAL PARA PAGINACIÓN
            // =========================
            var totalRecords = await query.CountAsync();

            var pedidos = await query
                .OrderByDescending(p => p.FechaPedido)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            ViewBag.PedidoId = pedidoId;
            ViewBag.Empresa = empresa;
            ViewBag.Semana = semana;

            return View(pedidos);
        }

        // =====================================================
        // GENERAR PEDIDOS POR SEMANA
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> GenerarPedidos(int semana)
        {
            // Buscar pedidos de clientes aprobados
            var pedidosClientes = await _context.PedidosCliente
                .Include(p => p.Detalles)
                .Where(p =>
                    p.Semana == semana &&
                    p.EstadoCredito == EstadoCredito.Aprobado &&
                    !p.FirmaBodega
                )
                .ToListAsync();

            if (!pedidosClientes.Any())
            {
                return BadRequest("No hay pedidos válidos para esta semana.");
            }

            // Crear PedidoMain
            var pedidoMain = new PedidoMain
            {
                Semana = semana,
                FechaGenerado = DateTime.UtcNow
            };

            _context.PedidosMain.Add(pedidoMain);
            await _context.SaveChangesAsync();

            // Agrupar detalles
            var detallesAgrupados = pedidosClientes
                .SelectMany(p => p.Detalles, (pedido, detalle) => new
                {
                    Empresa = pedido.Empresa,
                    detalle.ProveedorCedula,
                    detalle.CodigoProducto,
                    detalle.Color,
                    detalle.Talla,
                    detalle.Detalle,
                    detalle.Cantidad
                    

                })
                .GroupBy(x => new
                {
                    x.Empresa,
                    x.ProveedorCedula,
                    x.CodigoProducto,
                    x.Color,
                    x.Talla,
                    x.Detalle
                    
                })
                .Select(g => new
                {
                    Empresa = g.Key.Empresa,
                    ProveedorCedula = g.Key.ProveedorCedula,
                    CodigoProducto = g.Key.CodigoProducto,
                    Color = g.Key.Color,
                    Talla = g.Key.Talla,
                    Detalle = g.Key.Detalle, 
                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .ToList();

            // Agrupar por empresa + proveedor
            var pedidosProveedor = detallesAgrupados
                .GroupBy(x => new
                {
                    x.Empresa,
                    x.ProveedorCedula
                });

            foreach (var grupo in pedidosProveedor)
            {
                var pedidoProveedor = new PedidoProveedor
                {
                    PedidoMainId = pedidoMain.Id,
                    Empresa = grupo.Key.Empresa,
                    ProveedorCedula = grupo.Key.ProveedorCedula ?? 0,
                    Semana = semana,
                    FechaPedido = DateTime.UtcNow,
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

            return RedirectToAction("Index");
        }

        // =====================================================
        // DETALLE DEL PEDIDO PROVEEDOR
        // =====================================================
        public async Task<IActionResult> Details(int id)
        {
            var pedido = await _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            // traer todos los códigos de productos del pedido
            var codigos = pedido.Detalles
                .Select(d => d.CodigoProducto)
                .Distinct()
                .ToList();

            // traer los zapatos con sus imágenes
            var zapatos = await _context.Zapatos
                .Where(z => codigos.Contains(z.Codigo))
                .Include(z => z.Imagenes)
                .ToListAsync();

            ViewBag.Zapatos = zapatos;

            return View(pedido);
        }

        // =====================================================
        // ELIMINAR PEDIDO
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

        public async Task<IActionResult> ExportarExcel(int id)
        {
            var pedido = await _context.PedidosProveedor
                .Include(p => p.Proveedor)
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            var codigos = pedido.Detalles.Select(d => d.CodigoProducto).Distinct().ToList();

            var zapatos = await _context.Zapatos
                .Where(z => codigos.Contains(z.Codigo))
                .Include(z => z.Imagenes)
                .ToListAsync();

            var tallas = Enumerable.Range(15, 31).ToList();

            var grupos = pedido.Detalles
                .GroupBy(d => new { d.CodigoProducto, d.Color, d.Detalle })
                .ToList();

            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Pedido");

            // =========================
            // TITULO
            // =========================
            ws.Cell("A1").Value = "PEDIDO DE PRODUCCIÓN";
            ws.Range("A1:AK1").Merge();

            var titulo = ws.Range("A1:AK1");
            titulo.Style.Font.Bold = true;
            titulo.Style.Font.FontSize = 22;
            titulo.Style.Font.FontColor = XLColor.White;
            titulo.Style.Fill.BackgroundColor = XLColor.FromHtml("#2C3E50");
            titulo.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titulo.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Row(1).Height = 35;

            // =========================
            // INFORMACIÓN PEDIDO
            // =========================
            ws.Cell("A3").Value = "Proveedor:";
            ws.Cell("B3").Value = pedido.Proveedor?.Comercio;

            ws.Cell("A4").Value = "Empresa:";
            ws.Cell("B4").Value = pedido.Empresa;

            ws.Cell("A5").Value = "Semana:";
            ws.Cell("B5").Value = pedido.Semana;

            ws.Cell("A6").Value = "Fecha:";
            ws.Cell("B6").Value = pedido.FechaPedido.ToString("dd/MM/yyyy");

            var info = ws.Range("A3:D6");
            info.Style.Fill.BackgroundColor = XLColor.FromHtml("#ECF0F1");
            info.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;

            ws.Range("A3:A6").Style.Font.Bold = true;

            // =========================
            // ENCABEZADOS
            // =========================
            ws.Cell("A8").Value = "Imagen";
            ws.Cell("B8").Value = "Código";
            ws.Cell("C8").Value = "Color";
            ws.Cell("D8").Value = "Detalle";

            int col = 5;

            foreach (var talla in tallas)
            {
                ws.Cell(8, col).Value = talla;
                col++;
            }

            ws.Cell(8, col).Value = "Total";

            var header = ws.Range(8, 1, 8, col);

            header.Style.Font.Bold = true;
            header.Style.Font.FontColor = XLColor.White;
            header.Style.Fill.BackgroundColor = XLColor.FromHtml("#34495E");

            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            header.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            header.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            ws.Row(8).Height = 25;

            // =========================
            // DATOS
            // =========================
            int row = 9;
            int totalGeneral = 0;

            foreach (var g in grupos)
            {
                ws.Row(row).Height = 70;

                ws.Cell(row, 2).Value = g.Key.CodigoProducto;
                ws.Cell(row, 3).Value = g.Key.Color;
                ws.Cell(row, 4).Value = g.Key.Detalle;

                int colTalla = 5;
                int totalModelo = 0;

                foreach (var talla in tallas)
                {
                    var cantidad = g
                        .Where(x => x.Talla == talla.ToString())
                        .Sum(x => x.Cantidad);

                    if (cantidad > 0)
                        ws.Cell(row, colTalla).Value = cantidad;

                    ws.Cell(row, colTalla).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                    totalModelo += cantidad;

                    colTalla++;
                }

                // TOTAL POR MODELO
                ws.Cell(row, colTalla).Value = totalModelo;
                ws.Cell(row, colTalla).Style.Font.Bold = true;
                ws.Cell(row, colTalla).Style.Fill.BackgroundColor = XLColor.FromHtml("#D5F5E3");

                totalGeneral += totalModelo;

                // BORDES
                ws.Range(row, 1, row, colTalla).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                // FILAS ALTERNADAS
                if (row % 2 == 0)
                {
                    ws.Range(row, 1, row, colTalla).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8F9F9");
                }

                // =========================
                // IMAGEN
                // =========================
                var zapato = zapatos
                    .FirstOrDefault(z => z.Codigo == g.Key.CodigoProducto && z.Color == g.Key.Color);

                if (zapato != null && zapato.Imagenes.Any())
                {
                    var imgPath = Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot",
                        zapato.Imagenes.First().Url.TrimStart('/')
                    );

                    if (System.IO.File.Exists(imgPath))
                    {
                        ws.AddPicture(imgPath)
                            .MoveTo(ws.Cell(row, 1))
                            .Scale(0.4);
                    }
                }

                row++;
            }

            // =========================
            // TOTAL GENERAL
            // =========================
            ws.Cell(row + 1, 4).Value = "TOTAL GENERAL:";
            ws.Cell(row + 1, 4).Style.Font.Bold = true;

            ws.Cell(row + 1, 5).Value = totalGeneral;

            var totalRange = ws.Range(row + 1, 4, row + 1, 5);
            totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#F9E79F");
            totalRange.Style.Font.Bold = true;
            totalRange.Style.Font.FontSize = 14;

            // =========================
            // AJUSTES
            // =========================
            ws.Column(1).Width = 18;

            ws.Columns().AdjustToContents();

            ws.Columns(5, 35).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            ws.SheetView.FreezeRows(8);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"PedidoProveedor_{pedido.Id}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName
            );
        }
    }
}