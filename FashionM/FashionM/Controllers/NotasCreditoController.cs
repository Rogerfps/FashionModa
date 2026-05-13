using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class NotasCreditoController : Controller
    {
        private readonly AppDbContext _context;

        public NotasCreditoController(
            AppDbContext context)
        {
            _context = context;
        }

        // ========================================
        // INDEX
        // ========================================

        public async Task<IActionResult> Index()
        {
            var notas = await _context.NotasCredito
                .Include(n => n.Cliente)
                .Include(n => n.Empresa)
                .Include(n => n.Venta)
                .OrderByDescending(n => n.Fecha)
                .ToListAsync();

            return View(notas);
        }


        /// ========================================
        // DETAILS
        // ========================================

        public async Task<IActionResult> Details(int id)
        {
            var nota = await _context.NotasCredito
                .Include(n => n.Cliente)

                .Include(n => n.Empresa)

                .Include(n => n.Venta)

                .ThenInclude(v => v!.Detalles)

                .Include(n => n.Detalles)

                .ThenInclude(d => d.VentaDetalle)

                .FirstOrDefaultAsync(n => n.Id == id);

            if (nota == null)
                return NotFound();

            return View(nota);
        }


        // ========================================
        // CREATE
        // ========================================

        public async Task<IActionResult> Create(int ventaId)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empresa)
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            if (venta == null)
                return NotFound();

            foreach (var detalle in venta.Detalles)
            {
                int yaDevuelto =
                    await _context.NotaCreditoDetalles
                        .Where(n =>
                            n.VentaDetalleId == detalle.Id)
                        .SumAsync(x =>
                            x.CantidadDevuelta);

                ViewData[$"Disponible_{detalle.Id}"] =
                    detalle.Cantidad - yaDevuelto;
            }

            return View(venta);
        }


        // ========================================
        // CREATE POST
        // ========================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int ventaId,
            string motivo,
            decimal descuentoGlobal,
            List<int> detalleIds,
            List<int> cantidadesDevueltas,
            List<decimal> preciosCorregidos,
            List<decimal> descuentosLinea,
            List<int> eliminarLineaIds,
            List<string> observaciones)
        {
            var venta = await _context.Ventas
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == ventaId);

            if (venta == null)
                return NotFound();

            if (detalleIds == null ||
                !detalleIds.Any())
            {
                TempData["Error"] =
                    "Debe seleccionar al menos un producto.";

                return RedirectToAction(
                    nameof(Create),
                    new { ventaId });
            }

            var nota = new NotaCredito
            {
                VentaId = venta.Id,

                Fecha = DateTime.UtcNow,

                Motivo = motivo,

                TipoDocumento =
                    venta.TipoDocumento,

                ClienteCedula =
                    venta.ClienteCedula,

                EmpresaId =
                    venta.EmpresaId,

                Semana =
                    ISOWeek.GetWeekOfYear(
                        DateTime.UtcNow),

                Mes =
                    DateTime.UtcNow.Month,

                Año =
                    DateTime.UtcNow.Year,

                AgenteVenta =
                    venta.AgenteVenta,

                Estado = "ACTIVA",

                DescuentoGlobal =
                    descuentoGlobal
            };

            decimal subtotalGeneral = 0;

            // ========================================
            // RECORRER PRODUCTOS
            // ========================================

            for (int i = 0; i < detalleIds.Count; i++)
            {
                int detalleId =
                    detalleIds[i];

                int cantidadDevuelta =
                    cantidadesDevueltas.Count > i
                        ? cantidadesDevueltas[i]
                        : 0;

                decimal precioCorregido =
                    preciosCorregidos.Count > i
                        ? preciosCorregidos[i]
                        : 0;

                decimal descuentoLinea =
                    descuentosLinea.Count > i
                        ? descuentosLinea[i]
                        : 0;

                bool eliminado =
                    eliminarLineaIds != null &&
                    eliminarLineaIds.Contains(detalleId);

                string observacion =
                    observaciones.Count > i
                        ? observaciones[i]
                        : string.Empty;

                var detalleVenta =
                    venta.Detalles
                        .FirstOrDefault(d =>
                            d.Id == detalleId);

                if (detalleVenta == null)
                    continue;

                // ========================================
                // VALIDAR DEVOLUCIONES ANTERIORES
                // ========================================

                int yaDevuelto =
                    await _context.NotaCreditoDetalles
                        .Where(n =>
                            n.VentaDetalleId ==
                            detalleVenta.Id)
                        .SumAsync(x =>
                            x.CantidadDevuelta);

                int disponible =
                    detalleVenta.Cantidad -
                    yaDevuelto;

                // ========================================
                // ELIMINAR LÍNEA COMPLETA
                // ========================================

                if (eliminado)
                {
                    cantidadDevuelta =
                        disponible;
                }

                // ========================================
                // VALIDAR DISPONIBLE
                // ========================================

                if (cantidadDevuelta > disponible)
                {
                    TempData["Error"] =
                        $"La cantidad máxima disponible para devolver del producto {detalleVenta.InventarioCodigo} es {disponible}.";

                    return RedirectToAction(
                        nameof(Create),
                        new { ventaId });
                }

                // ========================================
                // SI NO DEVUELVE NADA
                // NO APLICAR DESCUENTO
                // ========================================

                if (cantidadDevuelta <= 0 &&
                    !eliminado)
                {
                    descuentoLinea = 0;
                }

                // ========================================
                // VALIDAR PRECIO
                // ========================================

                if (precioCorregido >
                    detalleVenta.PrecioUnitario)
                {
                    TempData["Error"] =
                        $"El precio corregido no puede ser mayor al original para el producto {detalleVenta.InventarioCodigo}.";

                    return RedirectToAction(
                        nameof(Create),
                        new { ventaId });
                }

                // ========================================
                // VALIDAR CAMBIOS
                // ========================================

                bool tieneCambios =
                    cantidadDevuelta > 0 ||
                    (
                        cantidadDevuelta > 0 &&
                        precioCorregido != detalleVenta.PrecioUnitario
                    ) ||
                    (
                        cantidadDevuelta > 0 &&
                        descuentoLinea > 0
                    ) ||
                    eliminado;

                if (!tieneCambios)
                    continue;

                // ========================================
                // PRECIO FINAL
                // ========================================

                decimal precioFinal =
                    precioCorregido > 0
                        ? precioCorregido
                        : detalleVenta.PrecioUnitario;

                // ========================================
                // SUBTOTAL
                // ========================================

                decimal subtotal =
                    cantidadDevuelta *
                    precioFinal;

                // ========================================
                // DESCUENTO LÍNEA
                // ========================================

                if (descuentoLinea > 0)
                {
                    subtotal -=
                        subtotal *
                        (descuentoLinea / 100);
                }

                subtotalGeneral += subtotal;

                // ========================================
                // DETALLE
                // ========================================

                nota.Detalles.Add(
                    new NotaCreditoDetalle
                    {
                        VentaDetalleId =
                            detalleVenta.Id,

                        InventarioCodigo =
                            detalleVenta.InventarioCodigo,

                        Color =
                            detalleVenta.Color,

                        Talla =
                            detalleVenta.Talla,

                        // CANTIDADES
                        CantidadOriginal =
                            detalleVenta.Cantidad,

                        CantidadDevuelta =
                            cantidadDevuelta,

                        // PRECIOS
                        PrecioOriginal =
                            detalleVenta.PrecioUnitario,

                        PrecioCorregido =
                            precioFinal,

                        // DESCUENTOS
                        DescuentoLinea =
                            descuentoLinea,

                        // ELIMINACIÓN
                        Eliminado =
                            eliminado,

                        // OBSERVACIONES
                        Observaciones =
                            observacion ?? string.Empty,

                        // TOTAL
                        SubTotal =
                            subtotal
                    });
            }

            // ========================================
            // VALIDAR DETALLES
            // ========================================

            if (!nota.Detalles.Any())
            {
                TempData["Error"] =
                    "La nota crédito no contiene ajustes válidos.";

                return RedirectToAction(
                    nameof(Create),
                    new { ventaId });
            }

            // ========================================
            // SUBTOTAL
            // ========================================

            nota.SubTotal =
                subtotalGeneral;

            // ========================================
            // DESCUENTO GLOBAL
            // ========================================

            decimal totalFinal =
                subtotalGeneral;

            if (descuentoGlobal > 0)
            {
                totalFinal -=
                    totalFinal *
                    (descuentoGlobal / 100);
            }

            nota.TotalDevuelto =
                totalFinal;

            // ========================================
            // GUARDAR
            // ========================================

            _context.NotasCredito.Add(nota);

            // ========================================
            // RECALCULAR ESTADO VENTA
            // ========================================

            decimal totalDevueltoVenta =
                await _context.NotasCredito
                    .Where(n =>
                        n.VentaId == venta.Id)
                    .SumAsync(x =>
                        x.TotalDevuelto);

            totalDevueltoVenta +=
                totalFinal;

            if (totalDevueltoVenta <= 0)
            {
                venta.Estado =
                    "ACTIVA";
            }
            else if (totalDevueltoVenta < venta.Total)
            {
                venta.Estado =
                    "DEVUELTA_PARCIAL";
            }
            else
            {
                venta.Estado =
                    "ANULADA";
            }

            // ========================================
            // SAVE
            // ========================================

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Nota de crédito generada correctamente.";

            return RedirectToAction(
                "Details",
                "Ventas",
                new { id = venta.Id });
        }
    }
}

