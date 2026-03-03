using FashionM.Data;
using FashionM.Enums;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    public class PedidoClienteController : Controller
    {
        private readonly AppDbContext _context;

        public PedidoClienteController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // LISTADO DE PEDIDOS
        // =====================================================
        public IActionResult Index()
        {
            var pedidos = _context.PedidosCliente
                .Include(p => p.Cliente)
                .OrderByDescending(p => p.FechaPedido)
                .ToList();

            return View(pedidos);
        }

        // =====================================================
        // DETALLE DEL PEDIDO
        // =====================================================
        public IActionResult Details(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.Proveedor)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // =====================================================
        // CREAR PEDIDO - GET
        // =====================================================
        public IActionResult Create()
        {
            var pedido = new PedidoCliente
            {
                FechaPedido = DateTime.UtcNow,
                FechaEntrega = DateTime.UtcNow.AddDays(60),
                EstadoCredito = EstadoCredito.Pendiente
            };

            return View(pedido);
        }

        // =====================================================
        // CREAR PEDIDO - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PedidoCliente pedido)
        {
            try
            {
                if (pedido.ClienteCedula == 0)
                    throw new Exception("ClienteCedula viene en 0");

                if (pedido.Detalles == null || !pedido.Detalles.Any())
                    throw new Exception("No hay detalles en el pedido");

                // 🔹 Asegurar UTC
                pedido.FechaPedido = DateTime.UtcNow;
                pedido.FechaEntrega = DateTime.UtcNow.AddDays(60);

                pedido.Total = pedido.Detalles.Sum(d => d.SubTotal);
                pedido.EstadoCredito = EstadoCredito.Pendiente;

                _context.PedidosCliente.Add(pedido);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                var sqlError = ex.InnerException?.Message;
                throw new Exception(sqlError);
            }
        }

        // =====================================================
        // BUSCAR CLIENTE (AJAX)
        // =====================================================
        [HttpGet]
        public IActionResult BuscarCliente(int cedula)
        {
            var cliente = _context.Clientes
                .Where(c => c.Cedula == cedula)
                .Select(c => new
                {
                    c.Cedula,
                    Nombre = c.Nombre + " " + c.Apellidos,
                    c.Direccion,
                    c.Transporte,
                    c.Correo,
                    c.Telefonos
                })
                .FirstOrDefault();

            if (cliente == null)
                return NotFound();

            return Json(cliente);
        }

        // =====================================================
        // 🔹 NUEVO: OBTENER PROVEEDORES
        // =====================================================
        [HttpGet]
        public IActionResult ObtenerProveedores()
        {
            var proveedores = _context.Proveedores
                .Where(p => p.Estado)
                .Select(p => new
                {
                    cedula = p.Cedula,
                    nombre = p.Comercio
                })
                .ToList();

            return Json(proveedores);
        }

        // =====================================================
        // 🔹 NUEVO: OBTENER CÓDIGOS POR PROVEEDOR
        // =====================================================
        [HttpGet]
        public IActionResult ObtenerCodigosPorProveedor(int proveedorCedula)
        {
            var codigos = _context.Zapatos
                .Where(z => z.ProveedorCedula == proveedorCedula)
                .Select(z => z.Codigo)
                .Distinct()
                .ToList();

            return Json(codigos);
        }

        // =====================================================
        // 🔹 NUEVO: OBTENER COLORES POR PROVEEDOR + CÓDIGO
        // =====================================================
        [HttpGet]
        public IActionResult ObtenerColoresPorCodigo(string codigo, int proveedorCedula)
        {
            var colores = _context.Zapatos
                .Where(z => z.Codigo == codigo && z.ProveedorCedula == proveedorCedula)
                .Select(z => z.Color)
                .Distinct()
                .ToList();

            return Json(colores);
        }

        // =====================================================
        // APROBAR CRÉDITO
        // =====================================================
        [HttpPost]
        public IActionResult AprobarCredito(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Cliente)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            // 🔹 Validacion básica de credito
            if (pedido.Total > pedido.Cliente.LimiteCredito)
            {
                pedido.EstadoCredito = EstadoCredito.Rechazado;
            }
            else
            {
                pedido.EstadoCredito = EstadoCredito.Aprobado;
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // RECHAZAR CRÉDITO
        // =====================================================
        [HttpPost]
        public IActionResult RechazarCredito(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);

            if (pedido == null)
                return NotFound();

            pedido.EstadoCredito = EstadoCredito.Rechazado;
            _context.SaveChanges();

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // RETENER CRÉDITO
        // =====================================================
        [HttpPost]
        public IActionResult RetenerCredito(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);

            if (pedido == null)
                return NotFound();

            pedido.EstadoCredito = EstadoCredito.Retenido;
            _context.SaveChanges();

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // FIRMAR BODEGA
        // =====================================================
        [HttpPost]
        public IActionResult FirmarBodega(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);

            if (pedido == null)
                return NotFound();

            if (pedido.EstadoCredito != EstadoCredito.Aprobado)
                return BadRequest("El pedido no tiene crédito aprobado.");

            pedido.FirmaBodega = true;
            _context.SaveChanges();

            return RedirectToAction(nameof(Details), new { id });
        }

        // =====================================================
        // ELIMINAR PEDIDO - GET
        // =====================================================
        public IActionResult Delete(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Cliente)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // =====================================================
        // ELIMINAR PEDIDO - POST
        // =====================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            _context.PedidosCliente.Remove(pedido);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}
