using ClosedXML.Excel;
using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
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

        public async Task<IActionResult> Index(
    string buscar,
    int? empresaId,
    string estado,
    string antiguedad)
        {
            // ========================================
            // EMPRESA SELECCIONADA
            // ========================================

            if (!Request.Query.ContainsKey("empresaId"))
            {
                empresaId = HttpContext.Session.GetInt32("EmpresaId");

                if (!empresaId.HasValue)
                {
                    return RedirectToAction(
                        "SeleccionarEmpresa",
                        "Home");
                }
            }

            // ========================================
            // CONSULTA BASE
            // ========================================

            var query = _context.CuentasPorCobrar
                .Include(c => c.Cliente)
                .Include(c => c.Empresa)
                .Include(c => c.Venta)
                    .ThenInclude(v => v!.NotasCredito)
                .Include(c => c.Pagos)
                .AsQueryable();

            // ========================================
            // FILTRO EMPRESA
            // ========================================

            if (empresaId.HasValue && empresaId.Value != 0)
            {
                query = query.Where(c =>
                    c.EmpresaId == empresaId.Value);
            }

            // ========================================
            // BUSQUEDA
            // ========================================

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim().ToLower();

                query = query.Where(c =>
                    c.Cliente!.Nombre.ToLower().Contains(buscar) ||
                    c.Cliente.Apellidos.ToLower().Contains(buscar) ||
                    c.Cliente.Cedula.ToString().Contains(buscar) ||
                    c.Cliente.Codigo.ToLower().Contains(buscar) ||
                    c.Cliente.Comercio.ToLower().Contains(buscar) ||
                    c.VentaId.ToString().Contains(buscar)
                );
            }

            // ========================================
            // OBTENER CUENTAS
            // ========================================

            var cuentas = await query.ToListAsync();

            // ========================================
            // FECHA ACTUAL
            // ========================================

            DateTime hoy = DateTime.UtcNow.Date;

            // ========================================
            // AGRUPAR POR CLIENTE + EMPRESA
            // ========================================

            var modelo = cuentas
                .GroupBy(x => new
                {
                    x.EmpresaId,

                    Empresa = x.Empresa!.Nombre,

                    x.ClienteCedula,

                    CodigoCliente = x.Cliente!.Codigo,

                    Nombre =
                        x.Cliente.Nombre +
                        " " +
                        x.Cliente.Apellidos,

                    Comercio = x.Cliente.Comercio
                })
                .Select(g =>
                {
                    // ========================================
                    // CALCULAR INFORMACIÓN DE CADA CUENTA
                    // ========================================

                    var cuentasCliente = g
                        .Select(c =>
                        {
                            // ========================================
                            // TOTAL PAGADO
                            // ========================================

                            decimal pagos =
                                c.Pagos.Sum(p => p.Monto);

                            // ========================================
                            // TOTAL NOTAS DE CRÉDITO
                            // ========================================

                            decimal notasCredito =
                                c.Venta?
                                    .NotasCredito
                                    .Sum(n => n.TotalDevuelto)
                                ?? 0;

                            // ========================================
                            // SALDO REAL
                            // ========================================

                            decimal saldo =
                                c.MontoOriginal
                                - pagos
                                - notasCredito
                                - c.DescuentoAplicado;

                            // ========================================
                            // SI EL SALDO ES MENOR A 1,
                            // SE CONSIDERA PAGADO
                            // ========================================

                            if (saldo < 1)
                            {
                                saldo = 0;
                            }

                            // ========================================
                            // FECHA DE INICIO DE LA DEUDA
                            //
                            // Usamos CuentaPorCobrar.Fecha
                            // ========================================

                            DateTime fechaDeuda = c.Fecha;

                            // ========================================
                            // DÍAS DE LA DEUDA
                            // ========================================

                            int diasDeuda = 0;

                            if (saldo > 0)
                            {
                                diasDeuda =
                                    Math.Max(
                                        0,
                                        (hoy - fechaDeuda.Date).Days);
                            }

                            // ========================================
                            // DÍAS VENCIDOS
                            // ========================================

                            int diasVencidos = 0;

                            if (saldo > 0 &&
                                c.FechaVencimiento.HasValue &&
                                c.FechaVencimiento.Value.Date < hoy)
                            {
                                diasVencidos =
                                    (
                                        hoy -
                                        c.FechaVencimiento.Value.Date
                                    ).Days;
                            }

                            // ========================================
                            // RESULTADO DE LA CUENTA
                            // ========================================

                            return new
                            {
                                Cuenta = c,

                                Pagos = pagos,

                                NotasCredito = notasCredito,

                                Saldo = saldo,

                                FechaDeuda = fechaDeuda,

                                DiasDeuda = diasDeuda,

                                DiasVencidos = diasVencidos
                            };
                        })
                        .ToList();

                    // ========================================
                    // SALDO TOTAL
                    // ========================================

                    decimal saldoTotal =
                        cuentasCliente.Sum(x => x.Saldo);

                    // ========================================
                    // CUENTAS PENDIENTES
                    // ========================================

                    var cuentasPendientes =
                        cuentasCliente
                            .Where(x => x.Saldo > 0)
                            .ToList();

                    // ========================================
                    // CUENTAS VENCIDAS
                    // ========================================

                    var cuentasVencidas =
                        cuentasPendientes
                            .Where(x =>
                                x.Cuenta.FechaVencimiento.HasValue &&
                                x.Cuenta.FechaVencimiento.Value.Date < hoy)
                            .ToList();

                    // ========================================
                    // SALDO VENCIDO
                    // ========================================

                    decimal saldoVencido =
                        cuentasVencidas.Sum(x => x.Saldo);

                    // ========================================
                    // SALDO PENDIENTE NO VENCIDO
                    // ========================================

                    decimal saldoPendiente =
                        cuentasPendientes
                            .Where(x =>
                                !x.Cuenta.FechaVencimiento.HasValue ||
                                x.Cuenta.FechaVencimiento.Value.Date >= hoy)
                            .Sum(x => x.Saldo);

                    // ========================================
                    // ANTIGÜEDAD DE LA DEUDA
                    //
                    // SOLO CUENTAS PENDIENTES
                    // ========================================

                    int diasDeuda = 0;

                    var cuentaPendienteMasAntigua =
                        cuentasPendientes
                            .OrderBy(x => x.FechaDeuda)
                            .FirstOrDefault();

                    if (cuentaPendienteMasAntigua != null)
                    {
                        diasDeuda =
                            cuentaPendienteMasAntigua.DiasDeuda;
                    }

                    // ========================================
                    // MAYOR CANTIDAD DE DÍAS VENCIDOS
                    // ========================================

                    int diasVencidos = 0;

                    if (cuentasVencidas.Any())
                    {
                        diasVencidos =
                            cuentasVencidas
                                .Max(x => x.DiasVencidos);
                    }

                    // ========================================
                    // ESTADO DEL CLIENTE
                    // ========================================

                    string estadoCliente;

                    if (saldoTotal <= 0)
                    {
                        estadoCliente = "Pagada";
                    }
                    else if (cuentasVencidas.Any())
                    {
                        estadoCliente = "Vencida";
                    }
                    else if (
                        cuentasPendientes.Any(x =>
                            x.Pagos > 0 &&
                            x.Saldo > 0))
                    {
                        estadoCliente = "Pago parcial";
                    }
                    else
                    {
                        estadoCliente = "Pendiente";
                    }

                    // ========================================
                    // RESULTADO
                    // ========================================

                    return new
                    {
                        EmpresaId =
                            g.Key.EmpresaId,

                        Empresa =
                            g.Key.Empresa,

                        ClienteCedula =
                            g.Key.ClienteCedula,

                        CodigoCliente =
                            g.Key.CodigoCliente,

                        Nombre =
                            g.Key.Nombre,

                        Comercio =
                            g.Key.Comercio,

                        CantidadFacturas =
                            g.Count(),

                        Saldo =
                            saldoTotal,

                        SaldoVencido =
                            saldoVencido,

                        SaldoPendiente =
                            saldoPendiente,

                        DiasDeuda =
                            diasDeuda,

                        DiasVencidos =
                            diasVencidos,

                        Estado =
                            estadoCliente
                    };
                })
                .ToList();

            // ========================================
            // FILTRO POR ESTADO
            // ========================================

            if (!string.IsNullOrWhiteSpace(estado))
            {
                modelo = modelo
                    .Where(x =>
                        x.Estado.Equals(
                            estado,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // ========================================
            // FILTRO POR ANTIGÜEDAD
            // ========================================

            if (!string.IsNullOrWhiteSpace(antiguedad))
            {
                if (antiguedad == "menos30")
                {
                    modelo = modelo
                        .Where(x => x.DiasDeuda < 30)
                        .ToList();
                }
                else if (antiguedad == "30a60")
                {
                    modelo = modelo
                        .Where(x =>
                            x.DiasDeuda >= 30 &&
                            x.DiasDeuda <= 60)
                        .ToList();
                }
                else if (antiguedad == "61a89")
                {
                    modelo = modelo
                        .Where(x =>
                            x.DiasDeuda >= 61 &&
                            x.DiasDeuda <= 89)
                        .ToList();
                }
                else if (antiguedad == "90a199")
                {
                    modelo = modelo
                        .Where(x =>
                            x.DiasDeuda >= 90 &&
                            x.DiasDeuda <= 199)
                        .ToList();
                }
                else if (antiguedad == "200mas")
                {
                    modelo = modelo
                        .Where(x =>
                            x.DiasDeuda >= 200)
                        .ToList();
                }
            }

            // ========================================
            // ORDEN
            // ========================================

            modelo = modelo
                .OrderBy(x => x.Empresa)
                .ThenByDescending(x => x.Saldo)
                .ToList();

            // ========================================
            // EMPRESAS
            // ========================================

            ViewBag.Empresas =
                await _context.Empresas
                    .OrderBy(x => x.Nombre)
                    .ToListAsync();

            // ========================================
            // VALORES DE LOS FILTROS
            // ========================================

            ViewBag.Estado =
                estado;

            ViewBag.Buscar =
                buscar;

            ViewBag.EmpresaId =
                empresaId;

            ViewBag.Antiguedad =
                antiguedad;

            // ========================================
            // RETURN
            // ========================================

            return View(modelo);
        }

        public async Task<IActionResult> ProximosVencimientos(
            string buscar,
            int? empresaId)
        {
            // ========================================
            // EMPRESA SELECCIONADA
            // ========================================

            if (!Request.Query.ContainsKey("empresaId"))
            {
                empresaId =
                    HttpContext.Session.GetInt32("EmpresaId");

                if (!empresaId.HasValue)
                {
                    return RedirectToAction(
                        "SeleccionarEmpresa",
                        "Home");
                }
            }

            // ========================================
            // CONSULTA BASE
            // ========================================

            var query = _context.CuentasPorCobrar
                .Include(c => c.Cliente)
                .Include(c => c.Empresa)
                .Include(c => c.Venta)
                    .ThenInclude(v => v!.NotasCredito)
                .Include(c => c.Pagos)
                .AsQueryable();

            // ========================================
            // FILTRO EMPRESA
            // ========================================

            if (empresaId.HasValue &&
                empresaId.Value != 0)
            {
                query = query.Where(c =>
                    c.EmpresaId == empresaId.Value);
            }

            // ========================================
            // BUSCAR CLIENTE
            // ========================================

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim().ToLower();

                query = query.Where(c =>
                    c.Cliente!.Nombre
                        .ToLower()
                        .Contains(buscar) ||

                    c.Cliente.Apellidos
                        .ToLower()
                        .Contains(buscar) ||

                    c.Cliente.Cedula
                        .ToString()
                        .Contains(buscar) ||

                    c.Cliente.Codigo
                        .ToLower()
                        .Contains(buscar) ||

                    c.Cliente.Comercio
                        .ToLower()
                        .Contains(buscar) ||

                    c.VentaId
                        .ToString()
                        .Contains(buscar)
                );
            }

            // ========================================
            // OBTENER CUENTAS
            // ========================================

            var cuentas =
                await query.ToListAsync();

            // ========================================
            // FECHA ACTUAL
            // ========================================

            DateTime hoy =
                DateTime.UtcNow.Date;

            // ========================================
            // DÍAS DE AVISO
            // ========================================

            int diasAvisoVencimiento = 7;

            DateTime fechaLimiteVencimiento =
                hoy.AddDays(
                    diasAvisoVencimiento);

            // ========================================
            // CUENTAS PRÓXIMAS A VENCER
            // ========================================

            var cuentasProximasAVencer =
                cuentas
                    .Select(c =>
                    {
                        // ========================================
                        // PAGOS
                        // ========================================

                        decimal pagos =
                            c.Pagos.Sum(p => p.Monto);

                        // ========================================
                        // NOTAS DE CRÉDITO
                        // ========================================

                        decimal notasCredito =
                            c.Venta?
                                .NotasCredito
                                .Sum(n => n.TotalDevuelto)
                            ?? 0;

                        // ========================================
                        // SALDO REAL
                        // ========================================

                        decimal saldo =
                            c.MontoOriginal
                            - pagos
                            - notasCredito
                            - c.DescuentoAplicado;

                        // ========================================
                        // SALDOS MENORES A 1
                        // SE CONSIDERAN PAGADOS
                        // ========================================

                        if (saldo < 1)
                        {
                            saldo = 0;
                        }

                        // ========================================
                        // DÍAS PARA VENCER
                        // ========================================

                        int diasParaVencer = 0;

                        if (c.FechaVencimiento.HasValue)
                        {
                            diasParaVencer =
                                (
                                    c.FechaVencimiento.Value.Date -
                                    hoy
                                ).Days;
                        }

                        // ========================================
                        // RESULTADO
                        // ========================================

                        return new
                        {
                            CuentaId =
                                c.Id,

                            VentaId =
                                c.VentaId,

                            ClienteCedula =
                                c.ClienteCedula,

                            CodigoCliente =
                                c.Cliente?.Codigo ?? "",

                            Cliente =
                                (c.Cliente?.Nombre ?? "") +
                                " " +
                                (c.Cliente?.Apellidos ?? ""),

                            Comercio =
                                c.Cliente?.Comercio ?? "",

                            EmpresaId =
                                c.EmpresaId,

                            Empresa =
                                c.Empresa?.Nombre ?? "",

                            Saldo =
                                saldo,

                            FechaVencimiento =
                                c.FechaVencimiento,

                            DiasParaVencer =
                                diasParaVencer
                        };
                    })
                    .Where(x =>
                        // Tiene saldo
                        x.Saldo > 0 &&

                        // Tiene vencimiento
                        x.FechaVencimiento.HasValue &&

                        // No está vencida
                        x.FechaVencimiento.Value.Date >= hoy &&

                        // Está dentro de los próximos 7 días
                        x.FechaVencimiento.Value.Date <=
                            fechaLimiteVencimiento
                    )
                    .OrderBy(x =>
                        x.DiasParaVencer)
                    .ThenBy(x =>
                        x.FechaVencimiento)
                    .ToList();

            // ========================================
            // EMPRESAS
            // ========================================

            ViewBag.Empresas =
                await _context.Empresas
                    .OrderBy(x => x.Nombre)
                    .ToListAsync();

            // ========================================
            // DATOS PARA LA VISTA
            // ========================================

            ViewBag.CuentasProximasAVencer =
                cuentasProximasAVencer;

            ViewBag.CantidadProximasAVencer =
                cuentasProximasAVencer.Count;

            ViewBag.DiasAvisoVencimiento =
                diasAvisoVencimiento;

            ViewBag.Buscar =
                buscar;

            ViewBag.EmpresaId =
                empresaId;

            // ========================================
            // RETURN
            // ========================================

            return View();
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
            // TOTAL PAGADO
            // ========================================

            decimal totalPagado =
                cuenta.Pagos.Sum(x => x.Monto);

            // ========================================
            // TOTAL NOTAS
            // ========================================

            decimal totalNotas =
                cuenta.Venta!.NotasCredito.Sum(x => x.TotalDevuelto);



            // ========================================
            // SALDO
            // ========================================

            decimal saldo =
                cuenta.MontoOriginal
                - totalPagado
                - totalNotas
                - cuenta.DescuentoAplicado;

            // ========================================
            // DESCUENTO AUTOMÁTICO
            // ========================================

            bool puedeAplicarDescuento =

                !cuenta.DescuentoOtorgado

                &&

                DateTime.UtcNow <=
                    cuenta.Fecha.AddDays(60);
            // ========================================
            // MONTO BASE PARA DESCUENTO
            // (YA CON NOTAS DE CRÉDITO)
            // ========================================

            decimal montoBase =

                cuenta.MontoOriginal
                - totalNotas;

            // ========================================
            // DESCUENTO (10%)
            // ========================================

            decimal montoDescuento =

                Math.Round(
                    montoBase * 0.10m,
                    0,
                    MidpointRounding.AwayFromZero);

            // ========================================
            // TOTAL NECESARIO PARA
            // OBTENER EL DESCUENTO
            // ========================================

            decimal montoConDescuento =

                montoBase
                - montoDescuento;

            // Total pagado después de este pago
            decimal totalPagadoConEstePago =

                totalPagado + monto;

            // ¿Obtiene el descuento?
            bool aplicarDescuento =
                puedeAplicarDescuento
                &&
                totalPagadoConEstePago == montoConDescuento;

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
            // REGISTRAR PAGO
            // ========================================

            var pago = new CuentaPorCobrarPago
            {
                CuentaPorCobrarId = cuenta.Id,

                Monto = monto,

                Fecha = DateTime.UtcNow,

                MetodoPago = metodoPago,

                Observacion = observacion ?? string.Empty
            };

            // ========================================
            // APLICAR DESCUENTO
            // ========================================

            if (aplicarDescuento)
            {
                cuenta.DescuentoAplicado =
                    montoDescuento;

                cuenta.DescuentoOtorgado =
                    true;

                cuenta.FechaDescuento =
                    DateTime.UtcNow;
            }

            _context.CuentasPorCobrarPagos.Add(pago);

            await _context.SaveChangesAsync();

            if (aplicarDescuento)
            {
                TempData["Success"] =
                    $"Pago registrado. Se aplicó automáticamente un descuento de ₡{montoDescuento:N0}.";
            }
            else
            {
                TempData["Success"] =
                    "Pago registrado correctamente.";
            }

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

        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<IActionResult> ExportarExcel(
    string buscar,
    int? empresaId,
    string estado,
    string antiguedad)
        {
            // ============================================================
            // EMPRESA
            // ============================================================

            if (!Request.Query.ContainsKey("empresaId"))
            {
                empresaId = HttpContext.Session.GetInt32("EmpresaId");

                if (!empresaId.HasValue)
                    return RedirectToAction("SeleccionarEmpresa", "Home");
            }

            // ============================================================
            // CONSULTA
            // ============================================================

            var query = _context.CuentasPorCobrar
                .Include(c => c.Cliente)
                .Include(c => c.Empresa)
                .Include(c => c.Venta)
                    .ThenInclude(v => v!.NotasCredito)
                .Include(c => c.Pagos)
                .AsQueryable();

            // ============================================================
            // FILTRO EMPRESA
            // ============================================================

            if (empresaId.HasValue && empresaId.Value != 0)
            {
                query = query.Where(c => c.EmpresaId == empresaId.Value);
            }

            // ============================================================
            // FILTRO BUSQUEDA
            // ============================================================

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                buscar = buscar.Trim().ToLower();

                query = query.Where(c =>
                    c.Cliente!.Nombre.ToLower().Contains(buscar) ||
                    c.Cliente.Apellidos.ToLower().Contains(buscar) ||
                    c.Cliente.Cedula.ToString().Contains(buscar) ||
                    c.Cliente.Codigo.ToLower().Contains(buscar) ||
                    c.Cliente.Comercio.ToLower().Contains(buscar) ||
                    c.VentaId.ToString().Contains(buscar)
                );
            }

            var cuentas = await query
                .OrderBy(c => c.Cliente!.Nombre)
                .ThenBy(c => c.FechaVencimiento)
                .ToListAsync();

            // ============================================================
            // OBTENER PROFORMAS
            // ============================================================

            var proformas = await _context.Proformas
                .Select(p => new
                {
                    p.Id,
                    p.Numero,
                    p.Fecha
                })
                .ToDictionaryAsync(p => p.Id);

            // ============================================================
            // FECHA ACTUAL
            // ============================================================

            DateTime hoy = DateTime.UtcNow.Date;

            // ============================================================
            // PROCESAR CUENTAS
            // ============================================================

            var cuentasFiltradas = new List<dynamic>();

            foreach (var c in cuentas)
            {
                // --------------------------------------------------------
                // PAGOS
                // --------------------------------------------------------

                decimal pagos = c.Pagos.Sum(p => p.Monto);

                // --------------------------------------------------------
                // NOTAS DE CREDITO
                // --------------------------------------------------------

                decimal notasCredito =
                    c.Venta?.NotasCredito.Sum(n => n.TotalDevuelto) ?? 0;

                // --------------------------------------------------------
                // SALDO
                // --------------------------------------------------------

                decimal saldo =
                    c.MontoOriginal
                    - pagos
                    - notasCredito
                    - c.DescuentoAplicado;

                if (saldo < 1)
                    saldo = 0;

                // --------------------------------------------------------
                // DIAS DE DEUDA
                // IMPORTANTE:
                // Este es el cálculo que utiliza el filtro de antigüedad
                // del INDEX.
                //
                // Se mantiene con CuentaPorCobrar.Fecha para que el
                // Excel respete exactamente el filtro del Index.
                // --------------------------------------------------------

                int diasDeuda = 0;

                if (saldo > 0)
                {
                    diasDeuda = Math.Max(
                        0,
                        (hoy - c.Fecha.Date).Days
                    );
                }

                // --------------------------------------------------------
                // DIAS DE MORA
                //
                // AQUÍ SE USA LA FECHA REAL DE LA PROFORMA.
                // --------------------------------------------------------

                DateTime fechaFactura = c.Fecha;
                int numeroProforma = 0;

                if (
                    c.Venta != null &&
                    c.Venta.TipoDocumento == "PROFORMA" &&
                    proformas.TryGetValue(
                        c.Venta.DocumentoId,
                        out var proforma)
                )
                {
                    numeroProforma = proforma.Numero;
                    fechaFactura = proforma.Fecha;
                }

                int diasMora = 0;

                if (saldo > 0)
                {
                    diasMora = Math.Max(
                        0,
                        (hoy - fechaFactura.Date).Days
                    );
                }

                // --------------------------------------------------------
                // ESTADO
                //
                // Se mantiene según la lógica del INDEX:
                // vencimiento real de la cuenta.
                // --------------------------------------------------------

                string estadoCuenta;

                bool estaVencida =
                    saldo > 0 &&
                    c.FechaVencimiento.HasValue &&
                    c.FechaVencimiento.Value.Date < hoy;

                if (saldo <= 0)
                {
                    estadoCuenta = "Pagada";
                }
                else if (estaVencida)
                {
                    estadoCuenta = "Vencida";
                }
                else if (pagos > 0)
                {
                    estadoCuenta = "Pago parcial";
                }
                else
                {
                    estadoCuenta = "Pendiente";
                }

                // --------------------------------------------------------
                // AGREGAR
                // --------------------------------------------------------

                cuentasFiltradas.Add(new
                {
                    Cuenta = c,
                    Pagos = pagos,
                    Saldo = saldo,
                    DiasDeuda = diasDeuda,
                    DiasMora = diasMora,
                    NumeroProforma = numeroProforma,
                    FechaFactura = fechaFactura,
                    Estado = estadoCuenta
                });
            }

            // ============================================================
            // FILTRO ESTADO
            // ============================================================

            if (!string.IsNullOrWhiteSpace(estado))
            {
                cuentasFiltradas = cuentasFiltradas
                    .Where(x =>
                        x.Estado.Equals(
                            estado,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // ============================================================
            // FILTRO ANTIGÜEDAD
            // ============================================================

            if (!string.IsNullOrWhiteSpace(antiguedad))
            {
                if (antiguedad == "menos30")
                {
                    cuentasFiltradas = cuentasFiltradas
                        .Where(x => x.DiasDeuda < 30)
                        .ToList();
                }
                else if (antiguedad == "30a60")
                {
                    cuentasFiltradas = cuentasFiltradas
                        .Where(x =>
                            x.DiasDeuda >= 30 &&
                            x.DiasDeuda <= 60)
                        .ToList();
                }
                else if (antiguedad == "61a89")
                {
                    cuentasFiltradas = cuentasFiltradas
                        .Where(x =>
                            x.DiasDeuda >= 61 &&
                            x.DiasDeuda <= 89)
                        .ToList();
                }
                else if (antiguedad == "90a199")
                {
                    cuentasFiltradas = cuentasFiltradas
                        .Where(x =>
                            x.DiasDeuda >= 90 &&
                            x.DiasDeuda <= 199)
                        .ToList();
                }
                else if (antiguedad == "200mas")
                {
                    cuentasFiltradas = cuentasFiltradas
                        .Where(x => x.DiasDeuda >= 200)
                        .ToList();
                }
            }

            // ============================================================
            // SOLO CUENTAS CON SALDO
            // ============================================================

            cuentasFiltradas = cuentasFiltradas
                .Where(x => x.Saldo > 0)
                .ToList();

            // ============================================================
            // ORDEN
            // ============================================================

            cuentasFiltradas = cuentasFiltradas
                .OrderBy(x => x.Cuenta.Cliente!.Zona)
                .ThenBy(x => x.Cuenta.Empresa!.Nombre)
                .ThenBy(x => x.Cuenta.Cliente!.Nombre)
                .ThenBy(x => x.FechaFactura)
                .ToList();

            // ============================================================
            // TOTALES GENERALES
            // ============================================================

            decimal totalMontoGeneral =
    cuentasFiltradas.Sum(x => (decimal)x.Cuenta.MontoOriginal);

            decimal totalAbonosGeneral =
                cuentasFiltradas.Sum(x => (decimal)x.Pagos);

            decimal totalSaldoGeneral =
                cuentasFiltradas.Sum(x => (decimal)x.Saldo);

            int mayorMoraGeneral =
                cuentasFiltradas.Any()
                    ? cuentasFiltradas.Max(x => x.DiasMora)
                    : 0;


            // ============================================================
            // CREAR EXCEL
            // ============================================================

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Cuentas por Cobrar");

            // ============================================================
            // COLORES
            // ============================================================

            var colorTitulo = XLColor.FromHtml("#7F1D1D");
            var colorEncabezado = XLColor.FromHtml("#B91C1C");
            var colorEncabezadoTexto = XLColor.White;
            var colorFilaPar = XLColor.FromHtml("#FFF1F2");
            var colorFilaImpar = XLColor.White;
            var colorBorde = XLColor.FromHtml("#E5E7EB");

            var colorMoraBaja = XLColor.FromHtml("#FEF3C7");
            var colorMoraMedia = XLColor.FromHtml("#FED7AA");
            var colorMoraAlta = XLColor.FromHtml("#FECACA");
            var colorMoraMuyAlta = XLColor.FromHtml("#991B1B");

            // ============================================================
            // NOMBRE DE EMPRESA
            // ============================================================

            string nombreEmpresa = "Todas las empresas";

            if (empresaId.HasValue && empresaId.Value != 0)
            {
                var empresaSeleccionada = await _context.Empresas
                    .Where(e => e.Id == empresaId.Value)
                    .Select(e => e.Nombre)
                    .FirstOrDefaultAsync();

                if (!string.IsNullOrWhiteSpace(empresaSeleccionada))
                {
                    nombreEmpresa = empresaSeleccionada;
                }
            }

            // ============================================================
            // TITULO
            // ============================================================

            worksheet.Cell(1, 1)
                .Value = "CUENTAS POR COBRAR";

            worksheet.Range(1, 1, 1, 9).Merge();

            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 18;
            worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;
            worksheet.Cell(1, 1).Style.Fill.BackgroundColor = colorTitulo;

            worksheet.Cell(1, 1)
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

            worksheet.Cell(1, 1)
                .Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

            worksheet.Row(1).Height = 30;

            // ============================================================
            // EMPRESA
            // ============================================================

            worksheet.Cell(2, 1)
                .Value = "Empresa:";

            worksheet.Cell(2, 1).Style.Font.Bold = true;

            worksheet.Cell(2, 2)
                .Value = nombreEmpresa;

            worksheet.Cell(2, 2).Style.Font.Bold = true;

            // ============================================================
            // FECHA GENERACION
            // ============================================================

            worksheet.Cell(2, 7)
                .Value = "Generado:";

            worksheet.Cell(2, 7).Style.Font.Bold = true;

            worksheet.Cell(2, 8)
                .Value = DateTime.Now;

            worksheet.Cell(2, 8)
                .Style.DateFormat.Format =
                    "dd/MM/yyyy HH:mm";

            // ============================================================
            // FILTROS APLICADOS
            // ============================================================

            worksheet.Cell(3, 1)
                .Value = "Filtros aplicados:";

            worksheet.Cell(3, 1).Style.Font.Bold = true;

            string textoFiltros = "";

            textoFiltros += string.IsNullOrWhiteSpace(buscar)
                ? "Cliente: Todos"
                : $"Cliente: {buscar}";

            textoFiltros += " | ";

            textoFiltros += string.IsNullOrWhiteSpace(estado)
                ? "Estado: Todos"
                : $"Estado: {estado}";

            textoFiltros += " | ";

            string textoAntiguedad = antiguedad switch
            {
                "menos30" => "Menos de 30 días",
                "30a60" => "30 a 60 días",
                "61a89" => "61 a 89 días",
                "90a199" => "90 a 199 días",
                "200mas" => "200 días o más",
                _ => "Todas"
            };

            textoFiltros += $"Antigüedad: {textoAntiguedad}";

            worksheet.Range(3, 2, 3, 9).Merge();

            worksheet.Cell(3, 2)
                .Value = textoFiltros;

            worksheet.Cell(3, 2)
                .Style.Font.Italic = true;

            // ============================================================
            // ENCABEZADOS
            // ============================================================

            int filaInicio = 5;

            string[] encabezados =
            {
        "Zona",
        "Empresa",
        "Cliente",
        "CodFactura",
        "FechaFactura",
        "MontoFactura",
        "Abono total",
        "SaldoPendiente",
        "Días de Mora"
    };

            for (int i = 0; i < encabezados.Length; i++)
            {
                var celda = worksheet.Cell(filaInicio, i + 1);

                celda.Value = encabezados[i];

                celda.Style.Font.Bold = true;
                celda.Style.Font.FontColor = colorEncabezadoTexto;
                celda.Style.Fill.BackgroundColor = colorEncabezado;

                celda.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                celda.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                celda.Style.Border.BottomBorder =
                    XLBorderStyleValues.Thick;

                celda.Style.Border.BottomBorderColor =
                    colorTitulo;
            }

            worksheet.Row(filaInicio).Height = 25;

            // ============================================================
            // DATOS AGRUPADOS POR ZONA
            // ============================================================

            int fila = filaInicio + 1;

            // Agrupar por zona
            var gruposPorZona = cuentasFiltradas
                .GroupBy(x => x.Cuenta.Cliente?.Zona ?? "Sin zona")
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var grupoZona in gruposPorZona)
            {
                // ========================================================
                // CUENTAS DE LA ZONA
                // ========================================================

                foreach (var item in grupoZona)
                {
                    var cuenta = item.Cuenta;

                    // ----------------------------------------------------
                    // ZONA
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 1)
                        .Value = cuenta.Cliente?.Zona ?? "";

                    // ----------------------------------------------------
                    // EMPRESA
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 2)
                        .Value = cuenta.Empresa?.Nombre ?? "";

                    // ----------------------------------------------------
                    // CLIENTE
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 3)
                        .Value =
                            $"{cuenta.Cliente?.Nombre ?? ""} " +
                            $"{cuenta.Cliente?.Apellidos ?? ""}"
                            .Trim();

                    // ----------------------------------------------------
                    // CODIGO FACTURA
                    // ----------------------------------------------------

                    string codigoFactura = "";

                    if (item.NumeroProforma > 0)
                    {
                        codigoFactura = $"P{item.NumeroProforma}";
                    }
                    else if (
                        cuenta.Venta != null &&
                        cuenta.Venta.TipoDocumento == "FACTURA_ELECTRONICA")
                    {
                        codigoFactura =
                            cuenta.Venta.DocumentoId.ToString();
                    }

                    worksheet.Cell(fila, 4)
                        .Value = codigoFactura;

                    // ----------------------------------------------------
                    // FECHA FACTURA
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 5)
                        .Value = item.FechaFactura;

                    worksheet.Cell(fila, 5)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy";

                    // ----------------------------------------------------
                    // MONTO FACTURA
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 6)
                        .Value = cuenta.MontoOriginal;

                    // ----------------------------------------------------
                    // ABONO TOTAL
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 7)
                        .Value = item.Pagos;

                    // ----------------------------------------------------
                    // SALDO PENDIENTE
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 8)
                        .Value = item.Saldo;

                    // ----------------------------------------------------
                    // DIAS DE MORA
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 9)
                        .Value = item.DiasMora;

                    // ----------------------------------------------------
                    // COLOR DE FILA
                    // ----------------------------------------------------

                    var colorFila =
                        fila % 2 == 0
                            ? colorFilaPar
                            : colorFilaImpar;

                    worksheet.Range(
                        fila,
                        1,
                        fila,
                        9
                    ).Style.Fill.BackgroundColor = colorFila;

                    // ----------------------------------------------------
                    // BORDES
                    // ----------------------------------------------------

                    worksheet.Range(
                        fila,
                        1,
                        fila,
                        9
                    ).Style.Border.BottomBorder =
                        XLBorderStyleValues.Thin;

                    worksheet.Range(
                        fila,
                        1,
                        fila,
                        9
                    ).Style.Border.BottomBorderColor =
                        colorBorde;

                    // ----------------------------------------------------
                    // COLOR DIAS DE MORA
                    // ----------------------------------------------------

                    var celdaMora = worksheet.Cell(fila, 9);

                    celdaMora.Style.Font.Bold = true;

                    if (item.DiasMora >= 90)
                    {
                        celdaMora.Style.Fill.BackgroundColor =
                            colorMoraMuyAlta;

                        celdaMora.Style.Font.FontColor =
                            XLColor.White;
                    }
                    else if (item.DiasMora >= 60)
                    {
                        celdaMora.Style.Fill.BackgroundColor =
                            colorMoraAlta;
                    }
                    else if (item.DiasMora >= 30)
                    {
                        celdaMora.Style.Fill.BackgroundColor =
                            colorMoraMedia;
                    }
                    else if (item.DiasMora > 0)
                    {
                        celdaMora.Style.Fill.BackgroundColor =
                            colorMoraBaja;
                    }

                    // ----------------------------------------------------
                    // ALINEACIONES
                    // ----------------------------------------------------

                    worksheet.Cell(fila, 1)
                        .Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(fila, 4)
                        .Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(fila, 5)
                        .Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                    worksheet.Cell(fila, 9)
                        .Style.Alignment.Horizontal =
                            XLAlignmentHorizontalValues.Center;

                    fila++;
                }

                // ========================================================
                // RESUMEN DE LA ZONA
                // ========================================================

                var fechaMasAntigua = grupoZona
                    .Min(x => x.FechaFactura);

                decimal montoZona = grupoZona
                     .Sum(x => (decimal)x.Cuenta.MontoOriginal);

                decimal abonosZona = grupoZona
                    .Sum(x => (decimal)x.Pagos);

                decimal saldoZona = grupoZona
                    .Sum(x => (decimal)x.Saldo);

                int mayorMoraZona = grupoZona
                    .Max(x => x.DiasMora);

                // --------------------------------------------------------
                // TEXTO DEL RESUMEN
                // --------------------------------------------------------

                worksheet.Cell(fila, 1)
                    .Value = $"RESUMEN ZONA: {grupoZona.Key}";

                worksheet.Range(
                    fila,
                    1,
                    fila,
                    4
                ).Merge();

                worksheet.Cell(fila, 5)
                    .Value = fechaMasAntigua;

                worksheet.Cell(fila, 5)
                    .Style.DateFormat.Format =
                        "dd/MM/yyyy";

                worksheet.Cell(fila, 6)
                    .Value = montoZona;

                worksheet.Cell(fila, 7)
                    .Value = abonosZona;

                worksheet.Cell(fila, 8)
                    .Value = saldoZona;

                worksheet.Cell(fila, 9)
                    .Value = mayorMoraZona;

                // --------------------------------------------------------
                // COLOR DEL RESUMEN
                // --------------------------------------------------------

                var rangoResumen = worksheet.Range(
                    fila,
                    1,
                    fila,
                    9
                );

                rangoResumen.Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#FDE68A");

                rangoResumen.Style.Font.Bold = true;

                rangoResumen.Style.Border.TopBorder =
                    XLBorderStyleValues.Medium;

                rangoResumen.Style.Border.BottomBorder =
                    XLBorderStyleValues.Medium;

                rangoResumen.Style.Border.TopBorderColor =
                    XLColor.FromHtml("#D97706");

                rangoResumen.Style.Border.BottomBorderColor =
                    XLColor.FromHtml("#D97706");

                // --------------------------------------------------------
                // ALINEACIONES RESUMEN
                // --------------------------------------------------------

                worksheet.Cell(fila, 5)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 9)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                // --------------------------------------------------------
                // COLOR ESPECIAL PARA MORA DEL RESUMEN
                // --------------------------------------------------------

                var celdaMoraResumen = worksheet.Cell(fila, 9);

                if (mayorMoraZona >= 90)
                {
                    celdaMoraResumen.Style.Fill.BackgroundColor =
                        colorMoraMuyAlta;

                    celdaMoraResumen.Style.Font.FontColor =
                        XLColor.White;
                }
                else if (mayorMoraZona >= 60)
                {
                    celdaMoraResumen.Style.Fill.BackgroundColor =
                        colorMoraAlta;
                }
                else if (mayorMoraZona >= 30)
                {
                    celdaMoraResumen.Style.Fill.BackgroundColor =
                        colorMoraMedia;
                }

                fila++;
            }

            // ============================================================
            // FORMATO MONETARIO
            // ============================================================

            if (fila > filaInicio + 1)
            {
                worksheet.Range(
                    filaInicio + 1,
                    6,
                    fila - 1,
                    8
                ).Style.NumberFormat.Format =
                    "#,##0.00";
            }

            // ============================================================
            // FILTRO AUTOMATICO
            // ============================================================

            worksheet.Range(
                filaInicio,
                1,
                Math.Max(fila - 1, filaInicio),
                encabezados.Length
            ).SetAutoFilter();

            // ============================================================
            // CONGELAR ENCABEZADO
            // ============================================================

            worksheet.SheetView.FreezeRows(filaInicio);

            // ============================================================
            // AJUSTE DE COLUMNAS
            // ============================================================

            worksheet.Columns()
                .AdjustToContents();

            // Limitar tamaños demasiado grandes

            worksheet.Column(1).Width = 18; // Zona
            worksheet.Column(2).Width = 24; // Empresa
            worksheet.Column(3).Width = 32; // Cliente
            worksheet.Column(4).Width = 16; // CodFactura
            worksheet.Column(5).Width = 16; // Fecha
            worksheet.Column(6).Width = 18; // Monto
            worksheet.Column(7).Width = 18; // Abono
            worksheet.Column(8).Width = 20; // Saldo
            worksheet.Column(9).Width = 16; // Mora

            // ============================================================
            // TOTAL AL FINAL
            // ============================================================

            if (fila > filaInicio + 1)
            {
                int filaTotal = fila + 1;

                worksheet.Cell(filaTotal, 5)
                    .Value = "TOTALES:";

                worksheet.Cell(filaTotal, 5)
                    .Style.Font.Bold = true;

                worksheet.Cell(filaTotal, 6)
                    .Value = totalMontoGeneral;

                worksheet.Cell(filaTotal, 7)
                    .Value = totalAbonosGeneral;

                worksheet.Cell(filaTotal, 8)
                    .Value = totalSaldoGeneral;

                worksheet.Cell(filaTotal, 9)
                    .Value = mayorMoraGeneral;

                worksheet.Range(
                    filaTotal,
                    5,
                    filaTotal,
                    8
                ).Style.Font.Bold = true;

                worksheet.Range(
                    filaTotal,
                    5,
                    filaTotal,
                    8
                ).Style.Fill.BackgroundColor =
                    XLColor.FromHtml("#FECAAD");

                worksheet.Range(
                    filaTotal,
                    6,
                    filaTotal,
                    8
                ).Style.NumberFormat.Format =
                    "#,##0.00";
            }

            // ============================================================
            // GUARDAR Y DESCARGAR
            // ============================================================

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            string nombreArchivo =
                $"CuentasPorCobrar_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo
            );
        }
    }
}
