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

            var codigos = pedido.Detalles.Select(d => d.CodigoProducto).Distinct().ToList();

            var zapatos = await _context.Zapatos
                .Where(z => codigos.Contains(z.Codigo))
                .Include(z => z.Imagenes)
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
    }
}
