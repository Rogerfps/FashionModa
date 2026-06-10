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
            var cuentas = await _context.CuentasPorCobrar

                .Include(c => c.Cliente)

                .Include(c => c.Empresa)

                .Include(c => c.Venta)
                    .ThenInclude(v => v!.NotasCredito)

                .Include(c => c.Pagos)

                .ToListAsync();

            var modelo = cuentas

                .GroupBy(x => new
                {
                    x.EmpresaId,

                    Empresa = x.Empresa!.Nombre,

                    x.ClienteCedula,

                    Nombre =
                        x.Cliente!.Nombre + " " +
                        x.Cliente.Apellidos,

                    Comercio =
                        x.Cliente.Comercio
                })

                .Select(g => new
                {
                    EmpresaId =
                        g.Key.EmpresaId,

                    Empresa =
                        g.Key.Empresa,

                    ClienteCedula =
                        g.Key.ClienteCedula,

                    Nombre =
                        g.Key.Nombre,

                    Comercio =
                        g.Key.Comercio,

                    CantidadFacturas =
                        g.Count(),

                    Saldo =
                        g.Sum(c =>
                        {
                            decimal pagos =
                                c.Pagos.Sum(p =>
                                    p.Monto);

                            decimal notas =
                                c.Venta?.NotasCredito
                                    .Sum(n =>
                                        n.TotalDevuelto) ?? 0;

                            decimal saldo =
                                c.MontoOriginal
                                - pagos
                                - notas;

                            // ========================================
                            // TODO SALDO MENOR A ₡1
                            // SE CONSIDERA PAGADO
                            // ========================================

                            return saldo < 1
                                ? 0
                                : saldo;
                        })
                })

                .OrderBy(x => x.Empresa)

                .ThenByDescending(x => x.Saldo)

                .ToList();

            ViewBag.Empresas =
                await _context.Empresas

                    .OrderBy(x => x.Nombre)

                    .ToListAsync();

            return View(modelo);
        }

        // ========================================
        // DETAILS CLIENTE
        // ========================================

        public async Task<IActionResult> Details(
            int clienteCedula,
            int empresaId)
        {
            var cuentas =
                await _context.CuentasPorCobrar

                    .Include(c => c.Cliente)

                    .Include(c => c.Empresa)

                    .Include(c => c.Venta)

                        .ThenInclude(v => v!.NotasCredito)

                    .Include(c => c.Pagos)

                    .Where(c =>

                        c.ClienteCedula == clienteCedula

                        &&

                        c.EmpresaId == empresaId

                    )

                    .OrderByDescending(c => c.Fecha)

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
                        clienteCedula = venta.ClienteCedula,
                        empresaId = venta.EmpresaId
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

                    EmpresaId =
                        venta.EmpresaId,

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
        public async Task<IActionResult> RegistrarPago(
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

                    .FirstOrDefaultAsync(c => c.Id == cuentaId);

            if (cuenta == null)
                return NotFound();

            // ========================================
            // SALDO ACTUAL
            // ========================================

            decimal totalPagado =
                cuenta.Pagos.Sum(x => x.Monto);

            decimal totalNotas =
                cuenta.Venta!.NotasCredito.Sum(x => x.TotalDevuelto);

            decimal saldo =
                cuenta.MontoOriginal
                - totalPagado
                - totalNotas;

            // ========================================
            // YA PAGADA
            // ========================================

            if (saldo < 1)
            {
                TempData["Success"] =
                    "La cuenta ya se considera pagada.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        clienteCedula = cuenta.ClienteCedula,
                        empresaId = cuenta.EmpresaId
                    });
            }

            // ========================================
            // VALIDAR MONTO
            // ========================================

            if (monto <= 0)
            {
                TempData["Error"] =
                    "El monto debe ser mayor a 0.";

                return RedirectToAction(
                    nameof(Details),
                    new
                    {
                        clienteCedula = cuenta.ClienteCedula,
                        empresaId = cuenta.EmpresaId
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
                        clienteCedula = cuenta.ClienteCedula,
                        empresaId = cuenta.EmpresaId
                    });
            }

            // ========================================
            // CREAR PAGO
            // ========================================

            var pago = new CuentaPorCobrarPago
            {
                CuentaPorCobrarId = cuenta.Id,

                Monto = monto,

                Fecha = DateTime.UtcNow,

                MetodoPago = metodoPago,

                Observacion = observacion ?? string.Empty
            };

            _context.CuentasPorCobrarPagos.Add(pago);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Pago registrado correctamente.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    clienteCedula = cuenta.ClienteCedula,
                    empresaId = cuenta.EmpresaId
                });
        }

        // ========================================
        // CREATE
        // ========================================

        public async Task<IActionResult> Create(int ventaId)
        {
            var venta = await _context.Ventas

                .Include(v => v.Cliente)

                .Include(v => v.Empresa)

                .Include(v => v.NotasCredito)

                .Include(v => v.CuentaPorCobrar)

                .FirstOrDefaultAsync(v => v.Id == ventaId);

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
                        clienteCedula = venta.ClienteCedula,
                        empresaId = venta.EmpresaId
                    });
            }

            // ========================================
            // TOTAL NOTAS
            // ========================================

            decimal totalNotas =
                venta.NotasCredito.Sum(x => x.TotalDevuelto);

            // ========================================
            // SALDO REAL
            // ========================================

            decimal saldoReal =
                venta.Total - totalNotas;

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

            var cuenta = new CuentaPorCobrar
            {
                VentaId = venta.Id,

                ClienteCedula = venta.ClienteCedula,

                EmpresaId = venta.EmpresaId,

                Empresa = venta.Empresa,

                MontoOriginal = venta.Total,

                Fecha = DateTime.UtcNow,

                DiasCredito = 30,

                FechaVencimiento = DateTime.UtcNow.AddDays(30),

                Estado = "PENDIENTE",

                Observaciones = $"Cuenta creada desde venta #{venta.Id}"
            };

            return View(cuenta);
        }

        // ========================================
        // CREATE POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CuentaPorCobrar cuenta)
        {
            var venta = await _context.Ventas

                .Include(v => v.CuentaPorCobrar)

                .Include(v => v.NotasCredito)

                .FirstOrDefaultAsync(v => v.Id == cuenta.VentaId);

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
                        clienteCedula = venta.ClienteCedula,
                        empresaId = venta.EmpresaId
                    });
            }

            // ========================================
            // TOTAL NOTAS
            // ========================================

            decimal totalNotas =
                venta.NotasCredito.Sum(x => x.TotalDevuelto);

            // ========================================
            // SALDO REAL
            // ========================================

            decimal saldoReal =
                venta.Total - totalNotas;

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
            // DATOS
            // ========================================

            cuenta.Fecha =
                DateTime.UtcNow;

            cuenta.FechaVencimiento =
                cuenta.Fecha.AddDays(cuenta.DiasCredito);

            cuenta.MontoOriginal =
                venta.Total;

            cuenta.EmpresaId =
                venta.EmpresaId;

            cuenta.Estado =
                "PENDIENTE";

            // ========================================
            // SAVE
            // ========================================

            _context.CuentasPorCobrar.Add(cuenta);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Cuenta por cobrar creada correctamente.";

            return RedirectToAction(
                nameof(Details),
                new
                {
                    clienteCedula = cuenta.ClienteCedula,
                    empresaId = cuenta.EmpresaId
                });
        }

    }
}
