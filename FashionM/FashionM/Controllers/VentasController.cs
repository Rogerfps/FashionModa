using FashionM.Data;
using FashionM.Models;
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
            // ========================================
            // 📦 CARGAR VENTAS
            // ========================================

            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .Include(v => v.Detalles)
                .Include(v => v.NotasCredito)
                    .ThenInclude(nc => nc.Detalles)
                .ToListAsync();


            // ========================================
            // 📅 NOMBRES DE LOS MESES
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


            // ========================================
            // 📅 AÑO Y MES SELECCIONADOS
            // ========================================

            int anioActual = anio ?? DateTime.Now.Year;
            int mesActual = mes ?? DateTime.Now.Month;

            ViewBag.AnioSeleccionado = anioActual;
            ViewBag.MesSeleccionado = mesActual;
            ViewBag.MesActual = nombresMeses[mesActual];


            // ========================================
            // 📅 AÑOS DISPONIBLES
            // ========================================

            ViewBag.Anios = ventas
                .Select(v => v.Año)
                .Distinct()
                .OrderByDescending(x => x)
                .ToList();


            // ========================================
            // 🧮 FUNCIÓN PARA CALCULAR LA VENTA NETA
            // ========================================
            //
            // Venta normal:
            //   Pares = CantidadZapatos
            //   Total = Total
            //
            // Venta con devolución parcial:
            //   Pares = CantidadZapatos - devueltos
            //   Total = Total - TotalDevuelto
            //
            // Venta anulada:
            //   Pares = 0
            //   Total = 0
            //
            // ========================================

            var ventasConMetricas = ventas
                .Where(v => v.Año == anioActual)
                .Select(v =>
                {
                    // ----------------------------------------
                    // Si la venta está ANULADA no cuenta
                    // ----------------------------------------

                    if (v.Estado != null &&
                        v.Estado.Trim().ToUpper() == "ANULADA")
                    {
                        return new
                        {
                            Venta = v,
                            ParesNetos = 0,
                            TotalNeto = 0m
                        };
                    }


                    // ----------------------------------------
                    // NOTAS DE CRÉDITO ACTIVAS
                    // ----------------------------------------

                    var notasCreditoActivas = v.NotasCredito?
                        .Where(nc =>
                            nc.Estado != null &&
                            nc.Estado.Trim().ToUpper() == "ACTIVA")
                        .ToList()
                        ?? new List<NotaCredito>();


                    // ----------------------------------------
                    // TOTAL DE PARES DEVUELTOS
                    // ----------------------------------------

                    int paresDevueltos = notasCreditoActivas
                        .SelectMany(nc => nc.Detalles)
                        .Sum(d => d.CantidadDevuelta);


                    // ----------------------------------------
                    // DINERO DEVUELTO
                    // ----------------------------------------

                    decimal dineroDevuelto = notasCreditoActivas
                        .Sum(nc => nc.TotalDevuelto);


                    // ----------------------------------------
                    // PARES NETOS
                    // ----------------------------------------

                    int paresNetos =
                        v.CantidadZapatos - paresDevueltos;


                    // Evitamos valores negativos
                    if (paresNetos < 0)
                        paresNetos = 0;


                    // ----------------------------------------
                    // TOTAL NETO
                    // ----------------------------------------

                    decimal totalNeto =
                        v.Total - dineroDevuelto;


                    // Evitamos valores negativos
                    if (totalNeto < 0)
                        totalNeto = 0;


                    return new
                    {
                        Venta = v,
                        ParesNetos = paresNetos,
                        TotalNeto = totalNeto
                    };

                })
                .ToList();


            // ========================================
            // 📅 PARES POR SEMANA
            // ========================================

            var ventasSemana = Enumerable.Range(1, 52)
                .Select(semana => new
                {
                    NumeroSemana = semana,

                    Semana = $"Semana {semana}",

                    Pares = ventasConMetricas
                        .Where(x => x.Venta.Semana == semana)
                        .Sum(x => x.ParesNetos),

                    Total = ventasConMetricas
                        .Where(x => x.Venta.Semana == semana)
                        .Sum(x => x.TotalNeto)
                })
                .OrderBy(x => x.NumeroSemana)
                .ToList();


            ViewBag.Semanas =
                ventasSemana.Select(x => x.Semana);

            ViewBag.SemanaPares =
                ventasSemana.Select(x => x.Pares);

            ViewBag.SemanaTotales =
                ventasSemana.Select(x => x.Total);


            // ========================================
            // 📆 PARES POR MES + EMPRESA
            // ========================================

            var empresasVentasMes = ventasConMetricas
                .GroupBy(x => new
                {
                    x.Venta.Mes,
                    Empresa = x.Venta.Empresa != null
                        ? x.Venta.Empresa.Nombre
                        : "Sin empresa"
                })
                .Select(g => new
                {
                    Mes = nombresMeses[g.Key.Mes],

                    Empresa = g.Key.Empresa,

                    Pares = g.Sum(x => x.ParesNetos),

                    Total = g.Sum(x => x.TotalNeto)
                })
                .ToList();


            ViewBag.EmpresasVentasMes =
                empresasVentasMes;


            // ========================================
            // 👤 VENTAS POR AGENTE
            // ========================================

            var agentesMes = ventasConMetricas
                .Where(x => x.Venta.Mes == mesActual)
                .GroupBy(x => x.Venta.AgenteVenta)
                .Select(g => new
                {
                    Agente = g.Key,

                    Pares = g.Sum(x => x.ParesNetos),

                    Total = g.Sum(x => x.TotalNeto)
                })
                .OrderByDescending(x => x.Pares)
                .ToList();


            ViewBag.Agentes =
                agentesMes.Select(x => x.Agente);

            ViewBag.AgentePares =
                agentesMes.Select(x => x.Pares);

            ViewBag.AgenteTotales =
                agentesMes.Select(x => x.Total);


            // ========================================
            // 🏢 VENTAS POR EMPRESA
            // ========================================

            var empresasMes = ventasConMetricas
                .Where(x => x.Venta.Mes == mesActual)
                .GroupBy(x =>
                    x.Venta.Empresa != null
                        ? x.Venta.Empresa.Nombre
                        : "Sin empresa")
                .Select(g => new
                {
                    Empresa = g.Key,

                    Pares = g.Sum(x => x.ParesNetos),

                    Total = g.Sum(x => x.TotalNeto)
                })
                .OrderByDescending(x => x.Pares)
                .ToList();


            ViewBag.Empresas =
                empresasMes.Select(x => x.Empresa);

            ViewBag.EmpresaPares =
                empresasMes.Select(x => x.Pares);

            ViewBag.EmpresaTotales =
                empresasMes.Select(x => x.Total);


            // ========================================
            // 📄 PROFORMA VS FACTURA
            // ========================================

            var tiposDocumento = ventasConMetricas
                .Where(x => x.Venta.Mes == mesActual)
                .GroupBy(x => x.Venta.TipoDocumento)
                .Select(g => new
                {
                    Tipo = g.Key,

                    Total = g.Sum(x => x.TotalNeto)
                })
                .ToList();


            ViewBag.TipoDocumento =
                tiposDocumento.Select(x => x.Tipo);

            ViewBag.TipoDocumentoTotales =
                tiposDocumento.Select(x => x.Total);


            // ========================================
            // 👟 TOP 10 PRODUCTOS MÁS VENDIDOS
            // ========================================
            //
            // Aquí también debemos descontar devoluciones.
            //
            // Ejemplo:
            //
            // Venta:
            //   Zapato ABC = 10 pares
            //
            // Nota crédito:
            //   Zapato ABC = 2 pares
            //
            // Resultado:
            //   Zapato ABC = 8 pares
            //
            // ========================================

            var productosVenta = ventasConMetricas
                .Where(x => x.Venta.Mes == mesActual)
                .SelectMany(x => x.Venta.Detalles.Select(d => new
                {
                    Producto = d.InventarioCodigo,

                    CantidadVenta = d.Cantidad,

                    VentaId = x.Venta.Id,

                    NotasCredito = x.Venta.NotasCredito
                        .Where(nc =>
                            nc.Estado != null &&
                            nc.Estado.Trim().ToUpper() == "ACTIVA")
                        .ToList()
                }))
                .ToList();


            var productosTop = productosVenta
                .GroupBy(x => x.Producto)
                .Select(g =>
                {
                    int vendidos = g.Sum(x => x.CantidadVenta);

                    int devueltos = g
                        .SelectMany(x => x.NotasCredito)
                        .SelectMany(nc => nc.Detalles)
                        .Where(d =>
                            d.InventarioCodigo == g.Key)
                        .Sum(d => d.CantidadDevuelta);

                    int cantidadNeta = vendidos - devueltos;

                    if (cantidadNeta < 0)
                        cantidadNeta = 0;

                    return new
                    {
                        Producto = g.Key,

                        Cantidad = cantidadNeta
                    };
                })
                .Where(x => x.Cantidad > 0)
                .OrderByDescending(x => x.Cantidad)
                .Take(10)
                .ToList();


            ViewBag.Productos =
                productosTop.Select(x => x.Producto);

            ViewBag.ProductosCantidad =
                productosTop.Select(x => x.Cantidad);


            // ========================================
            // 👤 TOP CLIENTES
            // ========================================

            var clientesTop = ventasConMetricas
                .Where(x => x.Venta.Mes == mesActual)
                .GroupBy(x =>
                    x.Venta.Cliente != null
                        ? $"{x.Venta.Cliente.Nombre} {x.Venta.Cliente.Apellidos}"
                        : "Sin cliente")
                .Select(g => new
                {
                    Cliente = g.Key,

                    Pares = g.Sum(x => x.ParesNetos),

                    Total = g.Sum(x => x.TotalNeto)
                })
                .Where(x => x.Pares > 0)
                .OrderByDescending(x => x.Pares)
                .Take(10)
                .ToList();


            ViewBag.Clientes =
                clientesTop.Select(x => x.Cliente);

            ViewBag.ClientesPares =
                clientesTop.Select(x => x.Pares);

            ViewBag.ClientesTotales =
                clientesTop.Select(x => x.Total);


            // ========================================
            // 📊 VISTA
            // ========================================

            return View(
                ventasConMetricas.Select(x => x.Venta).ToList()
            );
        }
    }
}
