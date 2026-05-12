using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
    [Authorize(Roles = "Admin,Secretaria")]
    public class EmpresasController : Controller
    {
        private readonly AppDbContext _context;

        public EmpresasController(AppDbContext context)
        {
            _context = context;
        }

        // LISTADO
        public async Task<IActionResult> Index()
        {
            return View(await _context.Empresas.ToListAsync());
        }

        // CREATE GET
        public IActionResult Create()
        {
            return View();
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Empresa empresa)
        {
            if (ModelState.IsValid)
            {
                _context.Add(empresa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(empresa);
        }

        // DETAILS
        public async Task<IActionResult> Details(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Proformas)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        // EDIT GET
        public async Task<IActionResult> Edit(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        // EDIT POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Empresa empresa)
        {
            if (id != empresa.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var empresaDb = await _context.Empresas.FindAsync(id);

                    if (empresaDb == null)
                        return NotFound();

                    // 🔥 ACTUALIZAR CAMPOS (forma segura)
                    empresaDb.Nombre = empresa.Nombre;
                    empresaDb.CedulaJuridica = empresa.CedulaJuridica;
                    empresaDb.Telefono = empresa.Telefono;
                    empresaDb.Direccion = empresa.Direccion;
                    empresaDb.CuentaBAC = empresa.CuentaBAC;
                    empresaDb.CuentaBCR = empresa.CuentaBCR;
                    empresaDb.CuentaBN = empresa.CuentaBN;
                    empresaDb.SimpeMovil = empresa.SimpeMovil;
                    empresaDb.Agentes = empresa.Agentes;

                    await _context.SaveChangesAsync();
                }
                catch (Exception)
                {
                    throw;
                }

                return RedirectToAction(nameof(Index));
            }

            return View(empresa);
        }

        // DELETE
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var empresa = await _context.Empresas
                .Include(e => e.Proformas)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null)
                return NotFound();

            if (empresa.Proformas.Any())
            {
                TempData["Error"] = "No se puede eliminar, tiene proformas asociadas";
                return RedirectToAction(nameof(Index));
            }

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }

}


