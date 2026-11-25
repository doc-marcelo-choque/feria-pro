using System.Security.Claims;
using FeriaPro.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Services;
public class SesionCookieEvents(AppDbContext db) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var idValido = int.TryParse(context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        var usuario = idValido ? await db.Usuarios.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id) : null;
        if (usuario == null || !usuario.Activo || usuario.Rol != context.Principal?.FindFirstValue(ClaimTypes.Role)
            || usuario.PasswordHash != context.Principal?.FindFirstValue("VersionCredencial"))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }
}
