using System.Security.Claims;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
namespace FeriaPro.Controllers;
public class AccountController(AppDbContext db) : Controller
{
    [AllowAnonymous] public IActionResult Login(string? returnUrl) => View(new LoginViewModel { ReturnUrl = returnUrl });
    [HttpPost, AllowAnonymous, EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var correo = model.Correo.Trim().ToLowerInvariant();
        var usuario = await db.Usuarios.SingleOrDefaultAsync(x => x.Correo == correo);
        var hasher = new PasswordHasher<Usuario>();
        var verificacion = usuario is { Activo: true }
            ? hasher.VerifyHashedPassword(usuario, usuario.PasswordHash, model.Password) : PasswordVerificationResult.Failed;
        if (verificacion == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Correo o contraseña incorrectos.");
            return View(model);
        }
        if (verificacion == PasswordVerificationResult.SuccessRehashNeeded)
        {
            usuario!.PasswordHash = hasher.HashPassword(usuario, model.Password);
            await db.SaveChangesAsync();
        }
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, usuario!.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Correo), new Claim(ClaimTypes.Role, usuario.Rol),
            new Claim("VersionCredencial", usuario.PasswordHash) };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = model.Recordarme });
        return Url.IsLocalUrl(model.ReturnUrl) ? LocalRedirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");
    }
    [HttpPost, Authorize] public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }
    [AllowAnonymous] public IActionResult AccessDenied() { Response.StatusCode = 403; return View(); }
}
