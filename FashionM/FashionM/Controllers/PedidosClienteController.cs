using FashionM.Data;
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
                FechaEntrega = DateTime.UtcNow.AddDays(60)
            };

            return View(pedido);
        }

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

                pedido.Total = pedido.Detalles.Sum(d => d.SubTotal);

                _context.PedidosCliente.Add(pedido);
                _context.SaveChanges();

                return RedirectToAction("Index");
            }
            catch (DbUpdateException ex)
            {
                // 🔴 ESTE ES EL ERROR REAL
                var sqlError = ex.InnerException?.Message;
                throw new Exception(sqlError);
            }
        }

        // =====================================================
        // CREAR PEDIDO - POST
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
        // FIRMAR CRÉDITO
        // =====================================================
        [HttpPost]
        public IActionResult FirmarCredito(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);

            if (pedido == null)
                return NotFound();

            pedido.FirmaCredito = true;
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

            if (!pedido.FirmaCredito)
                return BadRequest("El pedido no ha sido aprobado por credito.");

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

