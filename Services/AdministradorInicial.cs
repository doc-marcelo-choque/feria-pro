using System.ComponentModel.DataAnnotations;
using FeriaPro.Data;
using FeriaPro.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Services;
public static class AdministradorInicial
{
    public static async Task CrearAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        if (await db.Usuarios.AsNoTracking().AnyAsync(x => x.Rol == Roles.Administrador)) return;
        var correo = config["AdministradorInicial:Correo"]?.Trim().ToLowerInvariant();
        var password = config["AdministradorInicial:Password"];
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(password)) return;
        if (!new EmailAddressAttribute().IsValid(correo) || password.Length < 10)
            throw new InvalidOperationException("Configure un correo válido y una contraseña de al menos 10 caracteres para el administrador inicial.");
        if (await db.Usuarios.AnyAsync(x => x.Correo == correo))
            throw new InvalidOperationException("El correo del administrador inicial ya está en uso.");
        var usuario = new Usuario { Correo = correo, Rol = Roles.Administrador };
        usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, password);
        db.Usuarios.Add(usuario);
        await db.SaveChangesAsync();
    }
}
