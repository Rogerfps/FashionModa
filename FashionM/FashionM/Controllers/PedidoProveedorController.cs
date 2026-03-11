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
    }
}
    

