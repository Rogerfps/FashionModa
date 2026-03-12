using FashionM.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FashionM.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                false,
                false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            ViewBag.Error = "Usuario o contraseña incorrectos";
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }

        /*    Admin  */
        
        public async Task<IActionResult> CrearAdmin()
        {
            var user = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@admin.com",
                Nombre = "Administrador"
            };

            var result = await _userManager.CreateAsync(user, "Admin123*");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Admin");
                return Ok("Usuario Admin creado");
            }

            return Ok("Usuario Admin creado");
        }


        /*    Bodega  */
        /*public async Task<IActionResult> CrearUsuario()
        {
            var user = new ApplicationUser
            {
                UserName = "bodega",
                Email = "bodega@empresa.com",
                Nombre = "Usuario Bodega"
            };

            var result = await _userManager.CreateAsync(user, "Bodega123*");

            if (!result.Succeeded)
            {
                return Json(result.Errors);
            }

            await _userManager.AddToRoleAsync(user, "Bodega");

            return Content("Usuario creado correctamente");
        }*/

        /*    ALISSON   */
        /*
        public async Task<IActionResult> CrearSecretaria()
        {
            var user = new ApplicationUser
            {
                UserName = "Alisson",
                Email = "fashionshoescr24@gmail.com",
                Nombre = "Alisson"
            };

            var result = await _userManager.CreateAsync(user, "Mikasa21*");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Secretaria");
                return Content("Secretaria creada");
            }

            return Json(result.Errors);
        }*/

        /*    Yendry   */
        /*
        public async Task<IActionResult> CrearSecretaria()
        {
            var user = new ApplicationUser
            {
                UserName = "Yendry",
                Email = "jadelsgmoda@gmail.com",
                Nombre = "Yendry"
            };

            var result = await _userManager.CreateAsync(user, "Viay05*");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Secretaria");
                return Content("Secretaria creada");
            }

            return Json(result.Errors);
        }*/

        /*    Nuria   */
        /*
        public async Task<IActionResult> CrearSecretaria()
        {
            var user = new ApplicationUser
            {
                UserName = "Nuria",
                Email = "cocalzaplus@yahoo.com",
                Nombre = "Nuria"
            };

            var result = await _userManager.CreateAsync(user, "Nuria1970*");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Secretaria");
                return Content("Secretaria creada");
            }

            return Json(result.Errors);
        }*/

        /*    Karla   */
        /*
        public async Task<IActionResult> CrearSecretaria()
        {
            var user = new ApplicationUser
            {
                UserName = "Karla",
                Email = "kalu2225@gmail.com",
                Nombre = "Karla"
            };

            var result = await _userManager.CreateAsync(user, "Alivi2225*");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "Secretaria");
                return Content("Secretaria creada");
            }

            return Json(result.Errors);
        }*/

        public static async Task CrearRoles(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roles = { "Admin", "Secretaria", "Bodega" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }
    }
}
