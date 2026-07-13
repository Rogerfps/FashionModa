using FashionM.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class VentasController : Controller
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        // LISTADO
        public async Task<IActionResult> Index(int? empresaId)
        {
            if (!Request.Query.ContainsKey("empresaId"))
            {
                empresaId = HttpContext.Session.GetInt32("EmpresaId");

                if (!empresaId.HasValue)
                {
                    return RedirectToAction("SeleccionarEmpresa", "Home");
                }
            }

            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .AsQueryable();

            if (empresaId.HasValue && empresaId.Value != 0)
            {
                query = query.Where(v => v.EmpresaId == empresaId.Value);
            }

            var ventas = await query
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            ViewBag.Empresas = await _context.Empresas
                .OrderBy(x => x.Nombre)
                .ToListAsync();

            ViewBag.EmpresaId = empresaId;

            return View(ventas);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var venta = await _context.Ventas

                .Include(v => v.Cliente)

                .Include(v => v.Empresa)

                .Include(v => v.Detalles)

                .Include(v => v.NotasCredito)

                 .Include(v => v.CuentaPorCobrar)

                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
                return NotFound();

            return View(venta);
        }

        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<IActionResult> Graficos(int? mes, int? anio)
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .Include(v => v.Detalles)
                .ToListAsync();

            // ========================================
            // 📅 NOMBRES MESES
            // ========================================

            string[] nombresMeses =
            {
        "",
        "Enero",
        "Febrero",
        "Marzo",
        "Abril",
        "Mayo",
        "Junio",
        "Julio",
        "Agosto",
        "Septiembre",
        "Octubre",
        "Noviembre",
        "Diciembre"
    };

            int anioActual = anio ?? DateTime.Now.Year;
            int mesActual = mes ?? DateTime.Now.Month;

            ViewBag.AnioSeleccionado = anioActual;
            ViewBag.MesSeleccionado = mesActual;
            ViewBag.MesActual = nombresMeses[mesActual];

            ViewBag.Anios = ventas
                .Select(v => v.Año)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();

            //=========================================
            // FILTRO GENERAL
            //=========================================

            var ventasFiltradas = ventas
                .Where(v => v.Año == anioActual)
                .ToList();

            // ========================================
            // 📅 PARES POR SEMANA
            // ========================================

            var ventasSemana = Enumerable.Range(1, 52)
                .Select(semana => new
                {
                    NumeroSemana = semana,

                    Semana = $"Semana {semana}",

                    Pares = ventasFiltradas
                        .Where(v => v.Semana == semana)
                        .Sum(v => v.CantidadZapatos),

                    Total = ventasFiltradas
                        .Where(v => v.Semana == semana)
                        .Sum(v => v.Total)
                })
                .OrderBy(x => x.NumeroSemana)
                .ToList();

            ViewBag.Semanas = ventasSemana.Select(x => x.Semana);

            ViewBag.SemanaPares = ventasSemana.Select(x => x.Pares);

            ViewBag.SemanaTotales = ventasSemana.Select(x => x.Total);

            // ========================================
            // 📆 PARES POR MES + EMPRESA
            // ========================================

            var empresasVentasMes = ventasFiltradas
                .GroupBy(v => new
                {
                    v.Mes,
                    Empresa = v.Empresa!.Nombre
                })
                .Select(g => new
                {
                    Mes = nombresMeses[g.Key.Mes],

                    Empresa = g.Key.Empresa,

                    Pares = g.Sum(x => x.CantidadZapatos),

                    Total = g.Sum(x => x.Total)
                })
                .ToList();

            ViewBag.EmpresasVentasMes = empresasVentasMes;

            // ========================================
            // 👤 VENTAS POR AGENTE
            // ========================================

            var agentesMes = ventasFiltradas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => v.AgenteVenta)
                .Select(g => new
                {
                    Agente = g.Key,

                    Pares = g.Sum(x => x.CantidadZapatos),

                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Pares)
                .ToList();

            ViewBag.Agentes = agentesMes.Select(x => x.Agente);

            ViewBag.AgentePares = agentesMes.Select(x => x.Pares);

            ViewBag.AgenteTotales = agentesMes.Select(x => x.Total);

            // ========================================
            // 🏢 VENTAS POR EMPRESA
            // ========================================

            var empresasMes = ventasFiltradas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => v.Empresa!.Nombre)
                .Select(g => new
                {
                    Empresa = g.Key,

                    Pares = g.Sum(x => x.CantidadZapatos),

                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Pares)
                .ToList();

            ViewBag.Empresas = empresasMes.Select(x => x.Empresa);

            ViewBag.EmpresaPares = empresasMes.Select(x => x.Pares);

            ViewBag.EmpresaTotales = empresasMes.Select(x => x.Total);

            // ========================================
            // 📄 PROFORMA VS FACTURA
            // ========================================

            var tiposDocumento = ventasFiltradas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => v.TipoDocumento)
                .Select(g => new
                {
                    Tipo = g.Key,

                    Total = g.Sum(x => x.Total)
                })
                .ToList();

            ViewBag.TipoDocumento = tiposDocumento.Select(x => x.Tipo);

            ViewBag.TipoDocumentoTotales = tiposDocumento.Select(x => x.Total);

            // ========================================
            // 👟 PRODUCTOS MÁS VENDIDOS
            // ========================================

            var productosTop = ventasFiltradas
                .Where(v => v.Mes == mesActual)
                .SelectMany(v => v.Detalles)
                .GroupBy(d => d.InventarioCodigo)
                .Select(g => new
                {
                    Producto = g.Key,

                    Cantidad = g.Sum(x => x.Cantidad)
                })
                .OrderByDescending(x => x.Cantidad)
                .Take(15)
                .ToList();

            ViewBag.Productos = productosTop.Select(x => x.Producto);

            ViewBag.ProductosCantidad = productosTop.Select(x => x.Cantidad);

            // ========================================
            // 👤 CLIENTES TOP
            // ========================================

            var clientesTop = ventasFiltradas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => $"{v.Cliente!.Nombre} {v.Cliente.Apellidos}")
                .Select(g => new
                {
                    Cliente = g.Key,

                    Pares = g.Sum(x => x.CantidadZapatos),

                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Pares)
                .Take(10)
                .ToList();

            ViewBag.Clientes = clientesTop.Select(x => x.Cliente);

            ViewBag.ClientesPares = clientesTop.Select(x => x.Pares);

            ViewBag.ClientesTotales = clientesTop.Select(x => x.Total);

            return View(ventasFiltradas);
        }
    }
}


