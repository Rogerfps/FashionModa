using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    public class CuentasPorCobrarController : Controller
    {
        private readonly AppDbContext _context;

        public CuentasPorCobrarController(
            AppDbContext context)
        {
            _context = context;
        }

        // ========================================
        // INDEX
        // ========================================

        public async Task<IActionResult> Index()
        {
            var cuentas =
                await _context.CuentasPorCobrar

                    .Include(c => c.Cliente)

                    .Include(c => c.Venta)

                    .ThenInclude(v => v!.NotasCredito)

                    .Include(c => c.Pagos)

                    .ToListAsync();

            var clientesAgrupados =
                cuentas

                .GroupBy(x => new
                {
                    x.ClienteCedula,
                    Nombre =
                        x.Cliente!.Nombre
                        + " "
                        + x.Cliente.Apellidos
                })

                .Select(g => new
                {
                    ClienteCedula =
                        g.Key.ClienteCedula,

                    Nombre =
                        g.Key.Nombre,

                    Saldo =
                        g.Sum(c =>
                        {
                            decimal pagos =
                                c.Pagos.Sum(x =>
                                    x.Monto);

                            decimal notas =
                                c.Venta?.NotasCredito
                                    .Sum(x =>
                                        x.TotalDevuelto) ?? 0;

                            return c.MontoOriginal
                                - pagos
                                - notas;
                        })
                })

                .OrderByDescending(x =>
                    x.Saldo)

                .ToList();

            return View(clientesAgrupados);
        }

        // ========================================
        // DETAILS CLIENTE
        // ========================================

        public async Task<IActionResult> Details(
            int clienteCedula)
        {
            var cuentas =
                await _context.CuentasPorCobrar

                    .Include(c => c.Cliente)

                    .Include(c => c.Venta)

                    .ThenInclude(v => v!.NotasCredito)

                    .Include(c => c.Pagos)

                    .Where(c =>
                        c.ClienteCedula ==
                        clienteCedula)

                    .OrderByDescending(c =>
                        c.Fecha)

                    .ToListAsync();

            if (!cuentas.Any())
                return NotFound();

            return View(cuentas);
        }


        // ========================================
        // GENERAR CUENTA
        // ========================================

        public async Task<IActionResult>
            GenerarCuenta(int ventaId)
        {
            var venta =
                await _context.Ventas

                    .Include(v => v.Cliente)

                    .Include(v => v.NotasCredito)

                    .Include(v => v.CuentaPorCobrar)

                    .FirstOrDefaultAsync(v =>
                        v.Id == ventaId);

            if (venta == null)
                return NotFound();

            // ========================================
            // YA EXISTE
            // ========================================

            if (venta.CuentaPorCobrar != null)
            {
                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        clienteCedula =
                            venta.ClienteCedula
                    });
            }

            // ========================================
            // TOTAL NOTAS
            // ========================================

            decimal totalNotas =
                venta.NotasCredito
                    .Sum(x =>
                        x.TotalDevuelto);

            decimal montoOriginal =
                venta.Total - totalNotas;

            // ========================================
            // CREAR
            // ========================================

            var cuenta =
                new CuentaPorCobrar
                {
                    VentaId =
                        venta.Id,

                    ClienteCedula =
                        venta.ClienteCedula,

                    MontoOriginal =
                        montoOriginal,

                    Fecha =
                        DateTime.UtcNow,

                    FechaVencimiento =
                        DateTime.UtcNow
                            .AddDays(30),

                    Estado =
                        "PENDIENTE",

                    Observaciones =
                        $"Cuenta generada automáticamente desde venta #{venta.Id}"
                };

            _context.CuentasPorCobrar
                .Add(cuenta);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cuenta por cobrar generada correctamente.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    clienteCedula =
                        venta.ClienteCedula
                });
        }


        // ========================================
        // REGISTRAR PAGO
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            RegistrarPago(
                int cuentaId,
                decimal monto,
                string metodoPago,
                string observacion)
        {
            var cuenta =
                await _context.CuentasPorCobrar

                    .Include(c => c.Pagos)

                    .Include(c => c.Venta)

                    .ThenInclude(v => v!.NotasCredito)

                    .FirstOrDefaultAsync(c =>
                        c.Id == cuentaId);

            if (cuenta == null)
                return NotFound();

            // ========================================
            // SALDO ACTUAL
            // ========================================

            decimal totalPagado =
                cuenta.Pagos.Sum(x =>
                    x.Monto);

            decimal totalNotas =
                cuenta.Venta!.NotasCredito
                    .Sum(x =>
                        x.TotalDevuelto);

            decimal saldo =
                cuenta.MontoOriginal
                - totalPagado
                - totalNotas;

            // ========================================
            // VALIDAR
            // ========================================

            if (monto <= 0)
            {
                TempData["Error"] =
                    "El monto debe ser mayor a 0.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        clienteCedula =
                            cuenta.ClienteCedula
                    });
            }

            if (monto > saldo)
            {
                TempData["Error"] =
                    "El pago supera el saldo pendiente.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        clienteCedula =
                            cuenta.ClienteCedula
                    });
            }

            // ========================================
            // CREAR PAGO
            // ========================================

            var pago =
                new CuentaPorCobrarPago
                {
                    CuentaPorCobrarId =
                        cuenta.Id,

                    Monto =
                        monto,

                    Fecha =
                        DateTime.UtcNow,

                    MetodoPago =
                        metodoPago,

                    Observacion =
                        observacion ?? string.Empty
                };

            _context.CuentasPorCobrarPagos
                .Add(pago);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Pago registrado correctamente.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    clienteCedula =
                        cuenta.ClienteCedula
                });

        }

        // ========================================
        // CREATE
        // ========================================

        public async Task<IActionResult>
            Create(int ventaId)
        {
            var venta =
                await _context.Ventas

                    .Include(v => v.Cliente)

                    .Include(v => v.Empresa)

                    .Include(v => v.NotasCredito)

                    .Include(v => v.CuentaPorCobrar)

                    .FirstOrDefaultAsync(v =>
                        v.Id == ventaId);

            if (venta == null)
                return NotFound();

            // ========================================
            // YA EXISTE
            // ========================================

            if (venta.CuentaPorCobrar != null)
            {
                TempData["Error"] =
                    "La venta ya tiene una cuenta por cobrar.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        clienteCedula =
                            venta.ClienteCedula
                    });
            }

            // ========================================
            // TOTAL NOTAS
            // ========================================

            decimal totalNotas =
                venta.NotasCredito
                    .Sum(x =>
                        x.TotalDevuelto);

            // ========================================
            // SALDO REAL
            // ========================================

            decimal saldoReal =
                venta.Total - totalNotas;

            // ========================================
            // VALIDAR
            // ========================================

            if (saldoReal <= 0)
            {
                TempData["Error"] =
                    "La venta no tiene saldo pendiente.";

                return RedirectToAction(
                    "Details",
                    "Ventas",
                    new
                    {
                        id = venta.Id
                    });
            }

            // ========================================
            // MODELO
            // ========================================

            var cuenta =
                new CuentaPorCobrar
                {
                    VentaId =
                        venta.Id,

                    ClienteCedula =
                        venta.ClienteCedula,

                    // SIEMPRE TOTAL ORIGINAL
                    MontoOriginal =
                        venta.Total,

                    Fecha =
                        DateTime.UtcNow,

                    DiasCredito =
                        30,

                    FechaVencimiento =
                        DateTime.UtcNow
                            .AddDays(30),

                    Estado =
                        "PENDIENTE",

                    Observaciones =
                        $"Cuenta creada desde venta #{venta.Id}"
                };

            return View(cuenta);
        }


        // ========================================
        // CREATE POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult>
            Create(CuentaPorCobrar cuenta)
        {
            // ========================================
            // VALIDAR
            // ========================================

            var venta =
                await _context.Ventas

                    .Include(v => v.CuentaPorCobrar)

                    .Include(v => v.NotasCredito)

                    .FirstOrDefaultAsync(v =>
                        v.Id == cuenta.VentaId);

            if (venta == null)
                return NotFound();

            // ========================================
            // YA EXISTE
            // ========================================

            if (venta.CuentaPorCobrar != null)
            {
                TempData["Error"] =
                    "La venta ya tiene una cuenta por cobrar.";

                return RedirectToAction(
                    "Details",
                    "Ventas",
                    new
                    {
                        id = venta.Id
                    });
            }

            // ========================================
            // TOTAL NOTAS
            // ========================================

            decimal totalNotas =
                venta.NotasCredito
                    .Sum(x =>
                        x.TotalDevuelto);

            // ========================================
            // SALDO REAL
            // ========================================

            decimal saldoReal =
                venta.Total - totalNotas;

            // ========================================
            // VALIDAR SALDO
            // ========================================

            if (saldoReal <= 0)
            {
                TempData["Error"] =
                    "La venta no tiene saldo pendiente.";

                return RedirectToAction(
                    "Details",
                    "Ventas",
                    new
                    {
                        id = venta.Id
                    });
            }

            // ========================================
            // FECHA
            // ========================================

            cuenta.Fecha =
                DateTime.UtcNow;

            // ========================================
            // FECHA VENCIMIENTO
            // ========================================

            cuenta.FechaVencimiento =
                cuenta.Fecha
                    .AddDays(cuenta.DiasCredito);

            // ========================================
            // MONTO ORIGINAL
            // ========================================

            cuenta.MontoOriginal =
                venta.Total;

            // ========================================
            // ESTADO
            // ========================================

            cuenta.Estado =
                "PENDIENTE";

            // ========================================
            // SAVE
            // ========================================

            _context.CuentasPorCobrar
                .Add(cuenta);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cuenta por cobrar creada correctamente.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    clienteCedula =
                        cuenta.ClienteCedula
                });
        }

    }
}
