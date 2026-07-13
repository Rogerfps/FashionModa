using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace FashionM.Controllers
{

    [Authorize(Roles = "Admin,Secretaria")]
    public class ClienteSemanaController : Controller
    {
        private readonly AppDbContext _context;

        public ClienteSemanaController(AppDbContext context)
        {
            _context = context;
        }

        // =====================================
        // INDEX
        // =====================================
        public async Task<IActionResult> Index(int clienteCedula)
        {
            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == clienteCedula);

            if (cliente == null)
                return NotFound();

            ViewBag.Cliente = cliente;

            var lista = await _context.ClienteSemana
                .Where(x => x.ClienteCedula == clienteCedula)
                .OrderByDescending(x => x.Año)
                .ThenByDescending(x => x.Semana)
                .ToListAsync();

            return View(lista);
        }

        // =====================================
        // CREATE GET
        // =====================================
        public IActionResult Create(int clienteCedula)
        {
            var model = new ClienteSemana
            {
                ClienteCedula = clienteCedula
            };

            return View(model);
        }

        // =====================================
        // CREATE POST
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClienteSemana model)
        {
            var hoy = DateTime.UtcNow;

            int semana = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                hoy,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);

            model.Semana = semana;
            model.Año = hoy.Year;
            model.FechaRegistro = hoy;

            if (model.FechaVisita.HasValue)
            {
                model.FechaVisita = DateTime.SpecifyKind(
                    model.FechaVisita.Value,
                    DateTimeKind.Utc);
            }

            if (string.IsNullOrWhiteSpace(model.Usuario))
                model.Usuario = User.Identity?.Name ?? "";

            if (!ModelState.IsValid)
                return View(model);

            var existe = await _context.ClienteSemana.AnyAsync(x =>
                x.ClienteCedula == model.ClienteCedula &&
                x.Semana == model.Semana &&
                x.Año == model.Año);

            if (existe)
            {
                ModelState.AddModelError("", "Ya existe una visita registrada para esta semana.");
                return View(model);
            }

            _context.ClienteSemana.Add(model);

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.Cedula == model.ClienteCedula);

            if (cliente != null)
            {
                cliente.UltimaVisita = hoy;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new
            {
                clienteCedula = model.ClienteCedula
            });
        }

        // =====================================
        // EDIT GET
        // =====================================
        public async Task<IActionResult> Edit(int id)
        {
            var visita = await _context.ClienteSemana.FindAsync(id);

            if (visita == null)
                return NotFound();

            return View(visita);
        }

        // =====================================
        // EDIT POST
        // =====================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ClienteSemana model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // PostgreSQL requiere UTC para timestamp with time zone
            if (model.FechaVisita.HasValue)
            {
                model.FechaVisita = DateTime.SpecifyKind(
                    model.FechaVisita.Value,
                    DateTimeKind.Utc);
            }

            model.FechaRegistro = DateTime.SpecifyKind(
                model.FechaRegistro,
                DateTimeKind.Utc);

            _context.Update(model);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new
            {
                clienteCedula = model.ClienteCedula
            });
        }

        // =====================================
        // DELETE GET
        // =====================================
        public async Task<IActionResult> Delete(int id)
        {
            var visita = await _context.ClienteSemana
                .Include(x => x.Cliente)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (visita == null)
                return NotFound();

            return View(visita);
        }

        // =====================================
        // DELETE POST
        // =====================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var visita = await _context.ClienteSemana.FindAsync(id);

            if (visita == null)
                return NotFound();

            int cliente = visita.ClienteCedula;

            _context.ClienteSemana.Remove(visita);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new
            {
                clienteCedula = cliente
            });
        }
    }
}