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
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)

        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public IActionResult Login()
        {
            return View();
        }

        public async Task<IActionResult> Usuarios()
        {
            var usuarios = _userManager.Users.ToList();

            var lista = new List<object>();

            foreach (var u in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(u);

                lista.Add(new
                {
                    u.Id,
                    u.Nombre,
                    u.Email,
                    Roles = string.Join(", ", roles)
                });
            }

            return View(lista);
        }

        public IActionResult CrearUsuario()
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearUsuario(
    string nombre,
    string email,
    string password,
    List<string> roles)
        {
            // 🔥 volver a cargar roles si hay error
            ViewBag.Roles = _roleManager.Roles
                .Select(r => r.Name)
                .ToList();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                Nombre = nombre
            };

            var result = await _userManager.CreateAsync(user, password);

            // 🔥 VALIDACIONES BONITAS
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    var mensaje = error.Description;

                    if (error.Code == "DuplicateUserName")
                        mensaje = "Ya existe un usuario con ese correo.";

                    if (error.Code == "DuplicateEmail")
                        mensaje = "Ese correo ya está registrado.";

                    if (error.Code == "PasswordRequiresUpper")
                        mensaje = "La contraseña debe tener al menos una mayúscula.";

                    if (error.Code == "PasswordRequiresNonAlphanumeric")
                        mensaje = "La contraseña debe tener un carácter especial.";

                    if (error.Code == "PasswordTooShort")
                        mensaje = "La contraseña es demasiado corta.";

                    ModelState.AddModelError("", mensaje);
                }

                return View();
            }

            // 🔥 ROLES
            if (roles != null && roles.Any())
                await _userManager.AddToRolesAsync(user, roles);

            TempData["Success"] = "Usuario creado correctamente";

            return RedirectToAction("Usuarios");
        }

        public async Task<IActionResult> EditarUsuario(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            ViewBag.UserRoles = userRoles;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> EditarUsuario(string id, string nombre, List<string> roles)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            user.Nombre = nombre;

            await _userManager.UpdateAsync(user);

            // 🔥 actualizar roles
            var rolesActuales = await _userManager.GetRolesAsync(user);

            await _userManager.RemoveFromRolesAsync(user, rolesActuales);

            if (roles != null)
                await _userManager.AddToRolesAsync(user, roles);

            return RedirectToAction("Usuarios");
        }

        public async Task<IActionResult> EliminarUsuario(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);

            return RedirectToAction("Usuarios");
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


        /*    ===Bodega===  */
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

        /*    ===ALISSON===   */
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

        /*    ===Yendry===   */
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

        /*    ===Nuria===   */
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

        /*    ===Karla===   */
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

            string[] roles = { "Admin", "Secretaria", "Bodega", "Vendedor" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        public async Task<IActionResult> CrearRolesManual()
        {
            string[] roles = { "Admin", "Secretaria", "Bodega", "Vendedor" };

            foreach (var rol in roles)
            {
                if (!await _roleManager.RoleExistsAsync(rol))
                {
                    await _roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            return Content("Roles creados correctamente");
        }
    }
}
