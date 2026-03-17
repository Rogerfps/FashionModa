using FashionM.Data;
using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FashionM.Controllers
{
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
                _context.Update(empresa);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(empresa);
        }

        // DELETE
        public async Task<IActionResult> Delete(int id)
        {
            var empresa = await _context.Empresas.FirstOrDefaultAsync(e => e.Id == id);

            if (empresa == null)
                return NotFound();

            return View(empresa);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var empresa = await _context.Empresas.FindAsync(id);

            _context.Empresas.Remove(empresa);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}

