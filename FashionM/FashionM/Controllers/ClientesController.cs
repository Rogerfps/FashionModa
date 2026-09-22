using ClosedXML.Excel;
using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class ClientesController : Controller
    {
        private readonly AppDbContext _context;

        public ClientesController(AppDbContext context)
        {
            _context = context;
        }

        // ===============================
        // LISTAR
        // ===============================
        public async Task<IActionResult> Index(
            string buscar,
            bool? estado,
            string zona,
            string empresa,
            int page = 1)
        {
            int pageSize = 25;
            var clientes = _context.Clientes.AsQueryable();

            // 🔍 BUSQUEDA GENERAL
            // 🔍 BUSQUEDA GENERAL NORMALIZADA
            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string buscarNormalizado = buscar
                    .ToLower()
                    .Replace("-", "")
                    .Replace(" ", "");

                clientes = clientes.Where(c =>
                    c.Codigo.ToLower().Replace("-", "").Replace(" ", "").Contains(buscarNormalizado) ||
                    c.Nombre.ToLower().Replace("-", "").Replace(" ", "").Contains(buscarNormalizado) ||
                    c.Apellidos.ToLower().Replace("-", "").Replace(" ", "").Contains(buscarNormalizado) ||
                    c.Agente.ToLower().Replace("-", "").Replace(" ", "").Contains(buscarNormalizado) ||
                    c.Empresa.ToLower().Replace("-", "").Replace(" ", "").Contains(buscarNormalizado) ||
                    c.Cedula.ToString().Contains(buscarNormalizado) ||
                    (c.Telefonos != null &&
                     c.Telefonos.ToLower().Replace("-", "").Replace(" ", "").Contains(buscarNormalizado))
                );
            }

            // 🔘 FILTRO POR ESTADO
            if (estado.HasValue)
            {
                clientes = clientes.Where(c => c.Estado == estado.Value);
            }

            // 🔽 FILTRO POR ZONA
            if (!string.IsNullOrWhiteSpace(zona))
            {
                clientes = clientes.Where(c => c.Zona == zona);
            }

            // 🏢 FILTRO POR EMPRESA
            if (!string.IsNullOrWhiteSpace(empresa))
            {
                clientes = clientes.Where(c =>
                    c.Empresa != null &&
                    (
                        c.Empresa == empresa ||
                        c.Empresa.StartsWith(empresa + "|") ||
                        c.Empresa.EndsWith("|" + empresa) ||
                        c.Empresa.Contains("|" + empresa + "|")
                    )
                );
            }

            int totalRegistros = await clientes.CountAsync();

            var lista = await clientes
                .OrderBy(c => c.Codigo)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.TotalPaginas = (int)Math.Ceiling(totalRegistros / (double)pageSize);
            ViewBag.PaginaActual = page;


            // 🔽 CARGAR ZONAS
            ViewBag.Zonas = await _context.Clientes
                .Where(c => !string.IsNullOrEmpty(c.Zona))
                .Select(c => c.Zona)
                .Distinct()
                .OrderBy(z => z)
                .ToListAsync();

            // 🏢 CARGAR EMPRESAS
            ViewBag.Empresas = _context.Clientes
                .Where(c => !string.IsNullOrEmpty(c.Empresa))
                .AsEnumerable() 
                .SelectMany(c => c.Empresa.Split('|', StringSplitOptions.RemoveEmptyEntries))
                .Select(e => e.Trim())
                .Distinct()
                .OrderBy(e => e)
                .ToList();

            return View(lista);
        }

        // ===============================
        // CREATE (GET)
        // ===============================
        public IActionResult Create()
        {
            CargarTiposIdentificacion();
            return View();
        }

        // ===============================
        // CREATE (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Clientes cliente)
        {
            CargarTiposIdentificacion();

            bool existeCedula = await _context.Clientes
                .AnyAsync(c => c.Cedula == cliente.Cedula);

            if (existeCedula)
            {
                ModelState.AddModelError("Cedula", "La cédula ya existe.");
            }

            if (!ModelState.IsValid)
            {
                return View(cliente);
            }

            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // DETAILS
        // ===============================
        public async Task<IActionResult> Details(int id)
        {
            if (id == 0)
                return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        // ===============================
        // EDIT (GET)
        // ===============================
        public async Task<IActionResult> Edit(int id)
        {
            if (id == 0)
                return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == id);

            if (cliente == null)
                return NotFound();

            CargarTiposIdentificacion();
            return View(cliente);
        }

        // ===============================
        // EDIT (POST)
        // ===============================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Clientes cliente)
        {
            CargarTiposIdentificacion();

            if (!ModelState.IsValid)
                return View(cliente);

            try
            {
                _context.Update(cliente);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Clientes.Any(c => c.Cedula == cliente.Cedula))
                    return NotFound();

                throw;
            }

            return RedirectToAction(nameof(Index));
        }

        // ===============================
        // DELETE (GET)
        // ===============================
        public async Task<IActionResult> Delete(int id)
        {
            if (id == 0)
                return NotFound();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == id);

            if (cliente == null)
                return NotFound();

            return View(cliente);
        }

        // ===============================
        // DELETE (POST)
        // ===============================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int Cedula)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == Cedula);

            if (cliente == null)
                return NotFound();

            try
            {
                _context.Clientes.Remove(cliente);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty,
                    "No se puede eliminar este cliente porque tiene información relacionada (pedidos, proformas, ventas u otros registros).");

                return View("Delete", cliente);
            }
        }

        // ===============================
        // METODO PRIVADO
        // ===============================
        private void CargarTiposIdentificacion()
        {
            ViewBag.TiposIdentificacion = new List<SelectListItem>
        {
            new SelectListItem { Value = "Cedula Fisica", Text = "Cédula Física" },
            new SelectListItem { Value = "Cedula Juridica", Text = "Cédula Jurídica" },
            new SelectListItem { Value = "Dimex", Text = "DIMEX" },
            new SelectListItem { Value = "Nite", Text = "NITE" },
            new SelectListItem { Value = "Extranjero", Text = "Extranjero" }
        };
        }

        // ===============================
        // CAMBIAR CALIFICACIÓN
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CambiarCalificacion(int cedula, int calificacion)
        {
            if (calificacion < 1 || calificacion > 5)
                return BadRequest();

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == cedula);

            if (cliente == null)
                return NotFound();

            cliente.Calificacion = calificacion;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                calificacion = cliente.Calificacion
            });
        }

        // ===============================
        // CAMBIAR ÚLTIMA VISITA
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CambiarUltimaVisita(int cedula, DateTime fecha)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == cedula);

            if (cliente == null)
                return NotFound();

            // Evita el error de PostgreSQL con DateTime Local
            cliente.UltimaVisita = DateTime.SpecifyKind(fecha, DateTimeKind.Utc);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                fecha = cliente.UltimaVisita?.ToString("yyyy-MM-dd")
            });
        }

        // ===============================
        // CAMBIAR ALERTA
        // ===============================
        [HttpPost]
        public async Task<IActionResult> CambiarAlerta(int cedula)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == cedula);

            if (cliente == null)
                return NotFound();

            cliente.Alerta = !cliente.Alerta;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                alerta = cliente.Alerta
            });
        }

        // ===============================
        // EXPORTAR EXCEL
        // ===============================
        [Authorize(Roles = "Admin,Secretaria")]
        public async Task<IActionResult> ExportarExcel(
            string buscar,
            bool? estado,
            string zona,
            string empresa)
        {
            // ============================================================
            // CONSULTA BASE
            // ============================================================

            var query = _context.Clientes
                .AsQueryable();

            // ============================================================
            // BUSQUEDA GENERAL NORMALIZADA
            // MISMA LOGICA DEL INDEX
            // ============================================================

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                string buscarNormalizado = buscar
                    .ToLower()
                    .Replace("-", "")
                    .Replace(" ", "");

                query = query.Where(c =>
                    c.Codigo.ToLower()
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Contains(buscarNormalizado) ||

                    c.Nombre.ToLower()
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Contains(buscarNormalizado) ||

                    c.Apellidos.ToLower()
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Contains(buscarNormalizado) ||

                    c.Agente.ToLower()
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Contains(buscarNormalizado) ||

                    c.Empresa.ToLower()
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Contains(buscarNormalizado) ||

                    c.Cedula.ToString()
                        .Contains(buscarNormalizado) ||

                    (c.Telefonos != null &&
                     c.Telefonos.ToLower()
                        .Replace("-", "")
                        .Replace(" ", "")
                        .Contains(buscarNormalizado))
                );
            }

            // ============================================================
            // FILTRO POR ESTADO
            // ============================================================

            if (estado.HasValue)
            {
                query = query.Where(c =>
                    c.Estado == estado.Value);
            }

            // ============================================================
            // FILTRO POR ZONA
            // ============================================================

            if (!string.IsNullOrWhiteSpace(zona))
            {
                query = query.Where(c =>
                    c.Zona == zona);
            }

            // ============================================================
            // FILTRO POR EMPRESA
            // MISMA LOGICA DEL INDEX
            // ============================================================

            if (!string.IsNullOrWhiteSpace(empresa))
            {
                query = query.Where(c =>
                    c.Empresa != null &&
                    (
                        c.Empresa == empresa ||
                        c.Empresa.StartsWith(empresa + "|") ||
                        c.Empresa.EndsWith("|" + empresa) ||
                        c.Empresa.Contains("|" + empresa + "|")
                    )
                );
            }

            // ============================================================
            // OBTENER TODOS LOS CLIENTES FILTRADOS
            // SIN PAGINACION
            // ============================================================

            var clientes = await query
                .OrderBy(c => c.Codigo)
                .ToListAsync();

            // ============================================================
            // CREAR EXCEL
            // ============================================================

            using var workbook = new XLWorkbook();

            var worksheet =
                workbook.Worksheets.Add("Clientes");

            // ============================================================
            // COLORES
            // ============================================================

            var colorTitulo =
                XLColor.FromHtml("#1E3A8A");

            var colorEncabezado =
                XLColor.FromHtml("#2563EB");

            var colorFilaPar =
                XLColor.FromHtml("#EFF6FF");

            var colorFilaImpar =
                XLColor.White;

            var colorBorde =
                XLColor.FromHtml("#CBD5E1");

            var colorActivo =
                XLColor.FromHtml("#DCFCE7");

            var colorActivoTexto =
                XLColor.FromHtml("#166534");

            var colorInactivo =
                XLColor.FromHtml("#FEE2E2");

            var colorInactivoTexto =
                XLColor.FromHtml("#991B1B");

            var colorResumen =
                XLColor.FromHtml("#DBEAFE");

            // ============================================================
            // TITULO
            // ============================================================

            worksheet.Cell(1, 1)
                .Value = "LISTADO DE CLIENTES";

            worksheet.Range(1, 1, 1, 17)
                .Merge();

            worksheet.Cell(1, 1)
                .Style.Font.Bold = true;

            worksheet.Cell(1, 1)
                .Style.Font.FontSize = 18;

            worksheet.Cell(1, 1)
                .Style.Font.FontColor =
                    XLColor.White;

            worksheet.Cell(1, 1)
                .Style.Fill.BackgroundColor =
                    colorTitulo;

            worksheet.Cell(1, 1)
                .Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

            worksheet.Cell(1, 1)
                .Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

            worksheet.Row(1).Height = 32;

            // ============================================================
            // INFORMACION DEL REPORTE
            // ============================================================

            worksheet.Cell(2, 1)
                .Value = "Generado:";

            worksheet.Cell(2, 1)
                .Style.Font.Bold = true;

            worksheet.Cell(2, 2)
                .Value = DateTime.Now;

            worksheet.Cell(2, 2)
                .Style.DateFormat.Format =
                    "dd/MM/yyyy HH:mm";

            worksheet.Cell(2, 6)
                .Value = "Total clientes:";

            worksheet.Cell(2, 6)
                .Style.Font.Bold = true;

            worksheet.Cell(2, 7)
                .Value = clientes.Count;

            worksheet.Cell(2, 7)
                .Style.Font.Bold = true;

            // ============================================================
            // FILTROS APLICADOS
            // ============================================================

            worksheet.Cell(3, 1)
                .Value = "Filtros aplicados:";

            worksheet.Cell(3, 1)
                .Style.Font.Bold = true;

            string textoBusqueda =
                string.IsNullOrWhiteSpace(buscar)
                    ? "Todos"
                    : buscar;

            string textoEstado;

            if (!estado.HasValue)
            {
                textoEstado = "Todos";
            }
            else
            {
                textoEstado = estado.Value
                    ? "Activos"
                    : "Inactivos";
            }

            string textoZona =
                string.IsNullOrWhiteSpace(zona)
                    ? "Todas"
                    : zona;

            string textoEmpresa =
                string.IsNullOrWhiteSpace(empresa)
                    ? "Todas"
                    : empresa;

            string textoFiltros =
                $"Buscar: {textoBusqueda} | " +
                $"Estado: {textoEstado} | " +
                $"Zona: {textoZona} | " +
                $"Empresa: {textoEmpresa}";

            worksheet.Range(3, 2, 3, 17)
                .Merge();

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
        "Código",
        "Cédula",
        "Cliente",
        "Tipo ID",
        "Comercio",
        "Zona",
        "Empresa",
        "Correo",
        "Teléfonos",
        "Dirección",
        "Agente",
        "Transporte",
        "Actividad Económica",
        "Límite de Crédito",
        "Estado",
        "Calificación",
        "Última Visita"
    };

            for (int i = 0; i < encabezados.Length; i++)
            {
                var celda =
                    worksheet.Cell(
                        filaInicio,
                        i + 1);

                celda.Value =
                    encabezados[i];

                celda.Style.Font.Bold = true;

                celda.Style.Font.FontColor =
                    XLColor.White;

                celda.Style.Fill.BackgroundColor =
                    colorEncabezado;

                celda.Style.Alignment.Horizontal =
                    XLAlignmentHorizontalValues.Center;

                celda.Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;

                celda.Style.Border.BottomBorder =
                    XLBorderStyleValues.Thick;

                celda.Style.Border.BottomBorderColor =
                    colorTitulo;
            }

            worksheet.Row(filaInicio).Height = 28;

            // ============================================================
            // DATOS
            // ============================================================

            int fila = filaInicio + 1;

            foreach (var cliente in clientes)
            {
                // --------------------------------------------------------
                // CODIGO
                // --------------------------------------------------------

                worksheet.Cell(fila, 1)
                    .Value = cliente.Codigo;

                // --------------------------------------------------------
                // CEDULA
                // --------------------------------------------------------

                worksheet.Cell(fila, 2)
                    .Value = cliente.Cedula;

                // --------------------------------------------------------
                // CLIENTE
                // --------------------------------------------------------

                worksheet.Cell(fila, 3)
                    .Value =
                        $"{cliente.Nombre} {cliente.Apellidos}"
                        .Trim();

                // --------------------------------------------------------
                // TIPO ID
                // --------------------------------------------------------

                worksheet.Cell(fila, 4)
                    .Value = cliente.IDTipo;

                // --------------------------------------------------------
                // COMERCIO
                // --------------------------------------------------------

                worksheet.Cell(fila, 5)
                    .Value = cliente.Comercio;

                // --------------------------------------------------------
                // ZONA
                // --------------------------------------------------------

                worksheet.Cell(fila, 6)
                    .Value = cliente.Zona;

                // --------------------------------------------------------
                // EMPRESA
                // --------------------------------------------------------

                worksheet.Cell(fila, 7)
                    .Value = cliente.Empresa;

                // --------------------------------------------------------
                // CORREO
                // --------------------------------------------------------

                worksheet.Cell(fila, 8)
                    .Value = cliente.Correo;

                // --------------------------------------------------------
                // TELEFONOS
                // --------------------------------------------------------

                worksheet.Cell(fila, 9)
                    .Value = cliente.Telefonos;

                // --------------------------------------------------------
                // DIRECCION
                // --------------------------------------------------------

                worksheet.Cell(fila, 10)
                    .Value = cliente.Direccion;

                // --------------------------------------------------------
                // AGENTE
                // --------------------------------------------------------

                worksheet.Cell(fila, 11)
                    .Value = cliente.Agente;

                // --------------------------------------------------------
                // TRANSPORTE
                // --------------------------------------------------------

                worksheet.Cell(fila, 12)
                    .Value = cliente.Transporte;

                // --------------------------------------------------------
                // ACTIVIDAD ECONOMICA
                // --------------------------------------------------------

                worksheet.Cell(fila, 13)
                    .Value = cliente.ActividadEconomica;

                // --------------------------------------------------------
                // LIMITE DE CREDITO
                // --------------------------------------------------------

                worksheet.Cell(fila, 14)
                    .Value = cliente.LimiteCredito;

                // --------------------------------------------------------
                // ESTADO
                // --------------------------------------------------------

                var celdaEstado =
                    worksheet.Cell(fila, 15);

                celdaEstado.Value =
                    cliente.Estado
                        ? "ACTIVO"
                        : "INACTIVO";

                celdaEstado.Style.Font.Bold = true;

                if (cliente.Estado)
                {
                    celdaEstado.Style.Fill.BackgroundColor =
                        colorActivo;

                    celdaEstado.Style.Font.FontColor =
                        colorActivoTexto;
                }
                else
                {
                    celdaEstado.Style.Fill.BackgroundColor =
                        colorInactivo;

                    celdaEstado.Style.Font.FontColor =
                        colorInactivoTexto;
                }

                // --------------------------------------------------------
                // CALIFICACION
                // --------------------------------------------------------

                worksheet.Cell(fila, 16)
                    .Value = cliente.Calificacion;

                // --------------------------------------------------------
                // ULTIMA VISITA
                // --------------------------------------------------------

                if (cliente.UltimaVisita.HasValue)
                {
                    worksheet.Cell(fila, 17)
                        .Value = cliente.UltimaVisita.Value;

                    worksheet.Cell(fila, 17)
                        .Style.DateFormat.Format =
                            "dd/MM/yyyy";
                }
                else
                {
                    worksheet.Cell(fila, 17)
                        .Value = "Sin visita";
                }

                // --------------------------------------------------------
                // COLOR DE FILA
                // --------------------------------------------------------

                var colorFila =
                    fila % 2 == 0
                        ? colorFilaPar
                        : colorFilaImpar;

                worksheet.Range(
                    fila,
                    1,
                    fila,
                    encabezados.Length
                ).Style.Fill.BackgroundColor =
                    colorFila;

                // Restaurar color del estado después del color de fila

                if (cliente.Estado)
                {
                    worksheet.Cell(fila, 15)
                        .Style.Fill.BackgroundColor =
                            colorActivo;

                    worksheet.Cell(fila, 15)
                        .Style.Font.FontColor =
                            colorActivoTexto;
                }
                else
                {
                    worksheet.Cell(fila, 15)
                        .Style.Fill.BackgroundColor =
                            colorInactivo;

                    worksheet.Cell(fila, 15)
                        .Style.Font.FontColor =
                            colorInactivoTexto;
                }

                // --------------------------------------------------------
                // BORDES
                // --------------------------------------------------------

                worksheet.Range(
                    fila,
                    1,
                    fila,
                    encabezados.Length
                ).Style.Border.BottomBorder =
                    XLBorderStyleValues.Thin;

                worksheet.Range(
                    fila,
                    1,
                    fila,
                    encabezados.Length
                ).Style.Border.BottomBorderColor =
                    colorBorde;

                // --------------------------------------------------------
                // ALINEACIONES
                // --------------------------------------------------------

                worksheet.Cell(fila, 1)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 2)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 4)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 6)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 13)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Right;

                worksheet.Cell(fila, 14)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Right;

                worksheet.Cell(fila, 15)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 16)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                worksheet.Cell(fila, 17)
                    .Style.Alignment.Horizontal =
                        XLAlignmentHorizontalValues.Center;

                fila++;
            }

            // ============================================================
            // FORMATO MONETARIO
            // ============================================================

            if (fila > filaInicio + 1)
            {
                worksheet.Range(
                    filaInicio + 1,
                    14,
                    fila - 1,
                    14
                ).Style.NumberFormat.Format =
                    "#,##0.00";
            }

            // ============================================================
            // FORMATO ACTIVIDAD ECONOMICA
            // ============================================================

            if (fila > filaInicio + 1)
            {
                worksheet.Range(
                    filaInicio + 1,
                    13,
                    fila - 1,
                    13
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
            // CONGELAR ENCABEZADOS
            // ============================================================

            worksheet.SheetView.FreezeRows(
                filaInicio);

            // ============================================================
            // AJUSTAR COLUMNAS
            // ============================================================

            worksheet.Columns()
                .AdjustToContents();

            // Limitar columnas demasiado grandes

            worksheet.Column(1).Width = 15;
            worksheet.Column(2).Width = 16;
            worksheet.Column(3).Width = 32;
            worksheet.Column(4).Width = 14;
            worksheet.Column(5).Width = 28;
            worksheet.Column(6).Width = 18;
            worksheet.Column(7).Width = 28;
            worksheet.Column(8).Width = 30;
            worksheet.Column(9).Width = 22;
            worksheet.Column(10).Width = 40;
            worksheet.Column(11).Width = 22;
            worksheet.Column(12).Width = 22;
            worksheet.Column(13).Width = 20;
            worksheet.Column(14).Width = 20;
            worksheet.Column(15).Width = 15;
            worksheet.Column(16).Width = 15;
            worksheet.Column(17).Width = 18;

            // ============================================================
            // RESUMEN FINAL
            // ============================================================

            int filaResumen = fila + 1;

            worksheet.Cell(filaResumen, 1)
                .Value = "TOTAL DE CLIENTES:";

            worksheet.Cell(filaResumen, 1)
                .Style.Font.Bold = true;

            worksheet.Cell(filaResumen, 2)
                .Value = clientes.Count;

            worksheet.Cell(filaResumen, 2)
                .Style.Font.Bold = true;

            worksheet.Range(
                filaResumen,
                1,
                filaResumen,
                encabezados.Length
            ).Style.Fill.BackgroundColor =
                colorResumen;

            // ============================================================
            // GUARDAR EXCEL
            // ============================================================

            using var stream =
                new MemoryStream();

            workbook.SaveAs(stream);

            stream.Position = 0;

            string nombreArchivo =
                $"Clientes_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                nombreArchivo
            );
        }
    }
}

