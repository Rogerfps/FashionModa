using ClosedXML.Excel;
using FashionM.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;


namespace FashionM.Services
{
    public class ExcelInventarioService
    {
        private readonly IWebHostEnvironment _environment;

        public ExcelInventarioService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public byte[] Generar(List<Inventario> inventarios)
        {
            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Inventario");

            var tallas = inventarios
                .SelectMany(i => i.Tallas)
                .Select(t => t.Numero)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct()
                .OrderBy(n =>
                {
                    return int.TryParse(n, out int numero) ? numero : int.MaxValue;
                })
                .ToList();

            // =====================
            // TITULO
            // =====================

            ws.Cell("A1").Value = "CATÁLOGO DE INVENTARIO";

            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell("A1").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            int filaEncabezado = 3;

            ws.Cell(filaEncabezado, 1).Value = "Foto";
            ws.Cell(filaEncabezado, 2).Value = "Código";
            ws.Cell(filaEncabezado, 3).Value = "Color";
            ws.Cell(filaEncabezado, 4).Value = "Detalle";

            int columna = 5;

            foreach (var talla in tallas)
            {
                ws.Cell(filaEncabezado, columna).Value = talla;
                columna++;
            }

            ws.Cell(filaEncabezado, columna).Value = "Total";
            columna++;

            ws.Cell(filaEncabezado, columna).Value = "Precio";

            ws.Range(1, 1, 1, columna).Merge();

            var encabezado = ws.Range(filaEncabezado, 1, filaEncabezado, columna);

            encabezado.Style.Font.Bold = true;

            encabezado.Style.Fill.BackgroundColor = XLColor.ForestGreen;

            encabezado.Style.Font.FontColor = XLColor.White;

            encabezado.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            encabezado.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            encabezado.Style.Border.OutsideBorder =
                XLBorderStyleValues.Thin;

            encabezado.Style.Border.InsideBorder =
                XLBorderStyleValues.Thin;

            encabezado.SetAutoFilter();

            ws.SheetView.FreezeRows(3);



            // Aquí irá el título, encabezados y formato
            // (lo agregaremos en el siguiente paso)

            int fila = 4;

            foreach (var inventario in inventarios)
            {
                AgregarInventario(ws, inventario, tallas, ref fila);
            }

            
            ws.Column(1).Width = 18;
            ws.Column(2).Width = 15;
            ws.Column(3).Width = 18;

            for (int i = 4; i <= columna; i++)
            {
                ws.Column(i).AdjustToContents();
            }

            using var stream = new MemoryStream();

            workbook.SaveAs(stream);

            return stream.ToArray();
        }



        private void AgregarInventario(
            IXLWorksheet ws,
            Inventario inventario,
            List<string> tallasDisponibles,
            ref int fila)
        {
            var grupos = inventario.Tallas?
    .GroupBy(t => new
    {
        t.Color,
        t.Detalle
    })
    .OrderBy(g => g.Key.Color)
    .ThenBy(g => g.Key.Detalle)
    .ToList();

            if (grupos == null || !grupos.Any())
                return;

            int filaInicial = fila;
            int cantidadFilas = grupos.Count;
            int filaFinal = filaInicial + cantidadFilas - 1;

            // ===============================
            // CÓDIGO
            // ===============================

            ws.Cell(filaInicial, 2).Value = inventario.Codigo;

            if (cantidadFilas > 1)
                ws.Range(filaInicial, 2, filaFinal, 2).Merge();

            ws.Cell(filaInicial, 2).Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;

            ws.Cell(filaInicial, 2).Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            ws.Cell(filaInicial, 2).Style.Font.Bold = true;

            // ===============================
            // FOTO
            // ===============================

            var foto = inventario.Fotos?.FirstOrDefault();

            if (foto != null)
            {
                string ruta = Path.Combine(
                    _environment.WebRootPath,
                    foto.Ruta.TrimStart('/').Replace("/", Path.DirectorySeparatorChar.ToString())
                );

                if (File.Exists(ruta))
                {
                    if (cantidadFilas > 1)
                        ws.Range(filaInicial, 1, filaFinal, 1).Merge();

                    var imagen = ws.AddPicture(ruta);

                    imagen.MoveTo(ws.Cell(filaInicial, 1), 5, 5);

                    imagen.WithSize(90, 90);
                }
            }

            // Alto de filas

            for (int i = filaInicial; i <= filaFinal; i++)
                ws.Row(i).Height = 70;

            // ===============================
            // COLORES
            // ===============================

            foreach (var grupo in grupos)
            {
                ws.Cell(fila, 3).Value = grupo.Key.Color;
                ws.Cell(fila, 4).Value = grupo.Key.Detalle;

                var tallas = grupo.ToDictionary(
                    x => x.Numero,
                    x => x.Cantidad);

                int columna = 5;

                // Tallas 34-44

                foreach (var talla in tallasDisponibles)
                {
                    if (tallas.TryGetValue(talla, out int cantidad))
                    {
                        ws.Cell(fila, columna).Value = cantidad;
                    }
                    else
                    {
                        ws.Cell(fila, columna).Value = 0;
                    }

                    columna++;
                }

                // Total

                ws.Cell(fila, columna).Value = grupo.Sum(x => x.Cantidad);

                columna++;

                // Precio

                var primerItem = grupo.First();

                decimal precio = primerItem.Precio > 0
                    ? primerItem.Precio
                    : inventario.PrecioVenta;

                ws.Cell(fila, columna).Value = precio;

                ws.Cell(fila, columna).Style.NumberFormat.Format = "#,##0";

                // Centrar

                ws.Range(fila, 1, fila, columna)
                    .Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Range(fila, 1, fila, columna)
                    .Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

                // Bordes

                ws.Range(fila, 1, fila, columna)
                    .Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                ws.Range(fila, 1, fila, columna)
                    .Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                fila++;
            }
        }



    }
    
}

