using FashionM.Data;
using FashionM.Enums;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria,Vendedor")]
    public class PedidoClienteController : Controller
    {
        private readonly AppDbContext _context;

        public PedidoClienteController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index(string buscar, string empresa, int? semana, int page = 1)
        {
            int pageSize = 10;

            
            if (!Request.Query.ContainsKey("empresa"))
            {
                empresa = HttpContext.Session.GetString("EmpresaNombre");

                if (string.IsNullOrWhiteSpace(empresa))
                {
                    return RedirectToAction("SeleccionarEmpresa", "Home");
                }
            }

            var pedidos = _context.PedidosCliente
                .Include(p => p.Cliente)
                .AsQueryable();

            // =========================
            // 🔒 FILTRO POR ROL (CLAVE)
            // =========================
            if (User.IsInRole("Vendedor"))
            {
                var userName = User.Identity.Name;

                pedidos = pedidos.Where(p => p.Agente == userName);
            }

            // =========================
            // 🔍 BUSCADOR INTELIGENTE
            // =========================
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim().ToLower();
                bool esNumero = int.TryParse(buscar, out int idBuscado);

                pedidos = pedidos.Where(p =>
                    (esNumero && p.Id == idBuscado)
                    || p.Cliente.Nombre.ToLower().Contains(buscar)
                    || p.Cliente.Apellidos.ToLower().Contains(buscar)
                    || p.Cliente.Cedula.ToString().Contains(buscar)
                    || (p.Empresa != null && p.Empresa.ToLower().Contains(buscar))
                );
            }

            // =========================
            // 🏢 EMPRESA FLEXIBLE
            // =========================
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                var e = empresa.Trim();

                pedidos = pedidos.Where(p =>
                    p.Empresa != null &&
                    (
                        p.Empresa == e ||
                        EF.Functions.Like(p.Empresa, $"{e}|%") ||
                        EF.Functions.Like(p.Empresa, $"%|{e}") ||
                        EF.Functions.Like(p.Empresa, $"%|{e}|%")
                    )
                );
            }

            // =========================
            // 📅 SEMANA
            // =========================
            if (semana.HasValue)
            {
                pedidos = pedidos.Where(p => p.Semana == semana.Value);
            }

            // =========================
            // 📊 TOTAL
            // =========================
            int total = await pedidos.CountAsync();

            var lista = await pedidos
                .OrderByDescending(p => p.FechaPedido)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // =========================
            // 📄 PAGINACIÓN
            // =========================
            ViewBag.TotalPaginas = (int)Math.Ceiling(total / (double)pageSize);
            ViewBag.PaginaActual = page;

            // =========================
            // 🏢 EMPRESAS DINÁMICAS
            // =========================
            ViewBag.Empresas = _context.PedidosCliente
                .Where(p => p.Empresa != null && p.Empresa != "")
                .Select(p => p.Empresa)
                .AsEnumerable()
                .SelectMany(e => e.Split('|'))
                .Select(e => e.Trim())
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            ViewBag.Empresa = empresa;

            return View(lista);

        }

        // =====================================================
        // DETAILS
        // =====================================================
        public IActionResult Details(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            // 🔒 SEGURIDAD
            if (User.IsInRole("Vendedor") && pedido.Agente != User.Identity.Name)
                return Forbid();

            return View(pedido);
        }

        // =====================================================
        // CREATE GET
        // =====================================================
        public IActionResult Create()
        {
            return View(new PedidoCliente
            {
                FechaPedido = DateTime.UtcNow,
                FechaEntrega = DateTime.UtcNow.AddDays(60)
            });
        }

        // =====================================================
        // CREATE POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(PedidoCliente pedido)
        {
            ModelState.Remove("Cliente");

            if (pedido.ClienteCedula == 0)
                ModelState.AddModelError("", "Debe seleccionar un cliente.");

            if (pedido.Detalles == null || !pedido.Detalles.Any())
                ModelState.AddModelError("", "Debe agregar productos.");

            if (!ModelState.IsValid)
                return View(pedido);

            // 🔥 AGENTE AUTOMÁTICO
            pedido.Agente = User.Identity?.Name;

            pedido.FechaPedido = DateTime.UtcNow;
            pedido.FechaEntrega = DateTime.UtcNow.AddDays(60);
            pedido.Total = pedido.Detalles.Sum(d => d.SubTotal);

            _context.PedidosCliente.Add(pedido);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // 🔍 AUTOCOMPLETE CLIENTES
        // =====================================================
        [HttpGet]
        public IActionResult BuscarClientes(string term)
        {
            var clientes = _context.Clientes
                .Where(c =>
                    c.Nombre.Contains(term) ||
                    c.Apellidos.Contains(term) ||
                    c.Cedula.ToString().Contains(term)
                )
                .Take(10)
                .Select(c => new
                {
                    cedula = c.Cedula,
                    nombre = c.Nombre + " " + c.Apellidos
                })
                .ToList();

            return Json(clientes);
        }

        // =====================================================
        // 🔥 NUEVO: PROVEEDORES (CATALOGO)
        // =====================================================
        [HttpGet]
        public IActionResult ObtenerProveedores()
        {
            var proveedores = _context.ProveedoresCatalogo
                .Where(p => p.Activo)
                .Select(p => new
                {
                    id = p.Id,
                    nombre = p.Nombre
                })
                .ToList();

            return Json(proveedores);
        }

        // =====================================================
        // 🔥 NUEVO: CODIGOS POR PROVEEDOR
        // =====================================================
        [HttpGet]
        public IActionResult ObtenerCodigosPorProveedor(int proveedorId)
        {
            var codigos = _context.ZapatosProveedor
                .Where(z => z.ProveedorCatalogoId == proveedorId)
                .Select(z => z.Codigo)
                .Distinct()
                .ToList();

            return Json(codigos);
        }

        // =====================================================
        // 🔥 NUEVO: COLORES
        // =====================================================
        [HttpGet]
        public IActionResult ObtenerColoresPorCodigo(string codigo, int proveedorId)
        {
            var colores = _context.ZapatosProveedor
                .Where(z => z.Codigo == codigo && z.ProveedorCatalogoId == proveedorId)
                .SelectMany(z => z.Colores.Select(c => c.Nombre))
                .Distinct()
                .ToList();

            return Json(colores);
        }

        // =====================================================
        // EDIT GET
        // =====================================================
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<IActionResult> Edit(int id)
        {
            var pedido = await _context.PedidosCliente
                .Include(p => p.Cliente)
                .Include(p => p.Detalles)
                    .ThenInclude(d => d.ProveedorCatalogo)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // =====================================================
        // EDIT POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<IActionResult> Edit(PedidoCliente model)
        {
            ModelState.Remove("Cliente");

            if (!ModelState.IsValid)
                return View(model);

            var pedidoDb = await _context.PedidosCliente
                .Include(p => p.Detalles)
                .FirstOrDefaultAsync(p => p.Id == model.Id);

            if (pedidoDb == null)
                return NotFound();

            // =========================
            // DATOS PRINCIPALES
            // =========================
            pedidoDb.Empresa = model.Empresa;
            pedidoDb.Semana = model.Semana;
            pedidoDb.Observaciones = model.Observaciones;

            // =========================
            // 🔥 LIMPIAR Y REINSERTAR (más fácil y seguro)
            // =========================
            _context.PedidoClienteDetalles.RemoveRange(pedidoDb.Detalles);

            decimal total = 0;

            if (model.Detalles != null)
            {
                foreach (var d in model.Detalles)
                {
                    var nuevo = new PedidoClienteDetalle
                    {
                        PedidoClienteId = pedidoDb.Id,
                        ProveedorCatalogoId = d.ProveedorCatalogoId,
                        CodigoProducto = d.CodigoProducto,
                        Color = d.Color,
                        Talla = d.Talla,
                        Detalle = d.Detalle,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario,
                        EsStock = d.EsStock,
                    };

                    total += d.Cantidad * d.PrecioUnitario;

                    _context.PedidoClienteDetalles.Add(nuevo);
                }
            }

            pedidoDb.Total = total;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = pedidoDb.Id });
        }

        // =====================================================
        // DELETE
        // =====================================================
        [Authorize(Roles = "Admin,Secretaria")]
        public IActionResult Delete(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Cliente)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Secretaria")]
        public IActionResult DeleteConfirmed(int id)
        {
            var pedido = _context.PedidosCliente
                .Include(p => p.Detalles)
                .FirstOrDefault(p => p.Id == id);

            if (pedido == null)
                return NotFound();

            // 🔥 eliminar detalles primero (seguro)
            _context.PedidoClienteDetalles.RemoveRange(pedido.Detalles);

            _context.PedidosCliente.Remove(pedido);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ObtenerInfoZapato(string codigo, int proveedorId)
        {
            var zapato = _context.ZapatosProveedor
                .Include(z => z.Colores)
                .Include(z => z.Detalles)
                .Include(z => z.Tallas)
                .FirstOrDefault(z => z.Codigo == codigo && z.ProveedorCatalogoId == proveedorId);

            if (zapato == null)
                return Json(null);

            return Json(new
            {
                colores = zapato.Colores.Select(c => c.Nombre).ToList(),
                detalles = zapato.Detalles.Select(d => d.Nombre).ToList(),
                tallas = zapato.Tallas.Select(t => new
                {
                    numero = t.Numero,
                    precio = t.Precio
                }).ToList(),
                precioBase = zapato.PrecioVenta
            });
        }



        [HttpPost]
        public IActionResult ToggleEntregaDetalle([FromBody] JsonElement data)
        {
            int id = data.GetProperty("id").GetInt32();
            bool entregado = data.GetProperty("entregado").GetBoolean();

            var detalle = _context.PedidoClienteDetalles.Find(id);

            if (detalle == null)
                return NotFound();

            detalle.Entregado = entregado;

            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        public IActionResult ToggleStockDetalle([FromBody] JsonElement data)
        {
            int id = data.GetProperty("id").GetInt32();
            bool stock = data.GetProperty("stock").GetBoolean();

            var detalle = _context.PedidoClienteDetalles.Find(id);

            if (detalle == null)
                return NotFound();

            detalle.EsStock = stock;

            _context.SaveChanges();

            return Ok();
        }


        // =========================
        // SECRETARIA
        // =========================
        [HttpPost]
        [Authorize(Roles = "Admin,Secretaria")]
        public IActionResult ToggleSecretaria(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);
            if (pedido == null) return NotFound();

            pedido.AprobadoSecretaria = !pedido.AprobadoSecretaria;
            _context.SaveChanges();

            return Ok();
        }

        // =========================
        // CREDITO
        // =========================
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CambiarCredito(int id, string estado)
        {
            var pedido = _context.PedidosCliente.Find(id);
            if (pedido == null) return NotFound();

            pedido.EstadoCredito = Enum.Parse<EstadoCredito>(estado);
            _context.SaveChanges();

            return Ok();
        }

        // =========================
        // BODEGA
        // =========================
        [HttpPost]
        [Authorize(Roles = "Admin,Bodega")]
        public IActionResult ToggleBodega(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);
            if (pedido == null) return NotFound();

            pedido.FirmaBodega = !pedido.FirmaBodega;
            _context.SaveChanges();

            return Ok();
        }

        // =========================
        // ENTREGA (GLOBAL)
        // =========================
        [HttpPost]
        [Authorize(Roles = "Admin,Secretaria")]
        public IActionResult ToggleEntrega(int id)
        {
            var pedido = _context.PedidosCliente.Find(id);
            if (pedido == null) return NotFound();

            pedido.EstadoEntrega = !pedido.EstadoEntrega;
            _context.SaveChanges();

            return Ok();
        }

        // NUEVOS

        [HttpGet]
        public IActionResult BuscarProveedores(string term)
        {
            term = term?.Trim() ?? "";

            var query = _context.ProveedoresCatalogo
                .Where(p => p.Activo);

            if (User.IsInRole("Vendedor"))
            {
                var proveedores = query
                    .Where(p => p.Codigo.Contains(term))
                    .Take(10)
                    .Select(p => new
                    {
                        id = p.Id,
                        codigo = p.Codigo,
                        nombre = "" // 🔥 NO ENVIAR NOMBRE
                    })
                    .ToList();

                return Json(proveedores);
            }
            else
            {
                var proveedores = query
                    .Where(p =>
                        p.Nombre.Contains(term) ||
                        p.Codigo.Contains(term) ||
                        p.Cedula.Contains(term)
                    )
                    .Take(10)
                    .Select(p => new
                    {
                        id = p.Id,
                        codigo = p.Codigo,
                        nombre = p.Nombre
                    })
                    .ToList();

                return Json(proveedores);
            }
        }

        [HttpGet]
        public IActionResult BuscarCodigos(string term, int proveedorId)
        {
            term = term?.Trim() ?? "";

            var codigos = _context.ZapatosProveedor
                .Where(z => z.ProveedorCatalogoId == proveedorId &&
                            z.Codigo.Contains(term))
                .Select(z => z.Codigo)
                .Distinct()
                .Take(10)
                .ToList();

            return Json(codigos);
        }
    }
}


