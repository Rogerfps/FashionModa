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
        public async Task<IActionResult> Index()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();

            ViewBag.Empresas = await _context.Empresas
                .OrderBy(x => x.Nombre)
                .ToListAsync();

            return View(ventas);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (venta == null)
                return NotFound();

            return View(venta);
        }

        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<IActionResult> Graficos(int? mes)
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

            int mesActual =
                mes ?? DateTime.Now.Month;

            ViewBag.MesActual =
                nombresMeses[mesActual];

            ViewBag.MesSeleccionado =
                mesActual;


            // ========================================
            // 📅 VENTAS POR SEMANA
            // ========================================

            var ventasSemana = Enumerable.Range(1, 52)
                .Select(semana => new
                {
                    NumeroSemana = semana,

                    Semana = $"Semana {semana}",

                    Total = ventas
                        .Where(v => v.Semana == semana)
                        .Sum(v => v.Total)
                })
                .OrderBy(x => x.NumeroSemana)
                .ToList();

            ViewBag.Semanas =
                ventasSemana.Select(x => x.Semana);

            ViewBag.SemanaTotales =
                ventasSemana.Select(x => x.Total);


            // ========================================
            // 📆 VENTAS POR MES + EMPRESA
            // ========================================

            var empresasVentasMes = ventas
                .GroupBy(v => new
                {
                    v.Mes,
                    Empresa = v.Empresa!.Nombre
                })
                .Select(g => new
                {
                    Mes = nombresMeses[g.Key.Mes],

                    Empresa = g.Key.Empresa,

                    Total = g.Sum(x => x.Total)
                })
                .ToList();

            ViewBag.EmpresasVentasMes =
                empresasVentasMes;


            // ========================================
            // 👤 TOP AGENTES (MES ACTUAL)
            // ========================================

            var agentesMes = ventas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => v.AgenteVenta)
                .Select(g => new
                {
                    Agente = g.Key,

                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.Agentes =
                agentesMes.Select(x => x.Agente);

            ViewBag.AgenteTotales =
                agentesMes.Select(x => x.Total);


            // ========================================
            // 🏢 TOP EMPRESAS (MES ACTUAL)
            // ========================================

            var empresasMes = ventas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => v.Empresa!.Nombre)
                .Select(g => new
                {
                    Empresa = g.Key,

                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Total)
                .ToList();

            ViewBag.Empresas =
                empresasMes.Select(x => x.Empresa);

            ViewBag.EmpresaTotales =
                empresasMes.Select(x => x.Total);


            // ========================================
            // 📄 PROFORMA VS FACTURA
            // ========================================

            var tiposDocumento = ventas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v => v.TipoDocumento)
                .Select(g => new
                {
                    Tipo = g.Key,

                    Total = g.Sum(x => x.Total)
                })
                .ToList();

            ViewBag.TipoDocumento =
                tiposDocumento.Select(x => x.Tipo);

            ViewBag.TipoDocumentoTotales =
                tiposDocumento.Select(x => x.Total);


            // ========================================
            // 👟 PRODUCTOS MÁS VENDIDOS
            // ========================================

            var productosTop = ventas
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

            ViewBag.Productos =
                productosTop.Select(x => x.Producto);

            ViewBag.ProductosCantidad =
                productosTop.Select(x => x.Cantidad);


            // ========================================
            // 👤 CLIENTES TOP
            // ========================================

            var clientesTop = ventas
                .Where(v => v.Mes == mesActual)
                .GroupBy(v =>
                    $"{v.Cliente!.Nombre} {v.Cliente.Apellidos}"
                )
                .Select(g => new
                {
                    Cliente = g.Key,

                    Total = g.Sum(x => x.Total)
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToList();

            ViewBag.Clientes =
                clientesTop.Select(x => x.Cliente);

            ViewBag.ClientesTotales =
                clientesTop.Select(x => x.Total);


            return View(ventas);
        }
    }
}


