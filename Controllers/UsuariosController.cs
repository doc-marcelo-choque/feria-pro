using System.Security.Claims;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;

[Authorize(Roles = Roles.Administrador)]
public class UsuariosController(AppDbContext db) : Controller
{
    protected virtual string? TipoPerfil => null;
    private IQueryable<Usuario> Consulta() => db.Usuarios.Include(x => x.Estudiante).Include(x => x.Profesor)
        .Where(x => TipoPerfil == null || x.Rol == TipoPerfil);
    private void Preparar() { ViewBag.TipoPerfil = TipoPerfil; ViewBag.Modulo = TipoPerfil == null ? "Usuarios" : TipoPerfil == Roles.Estudiante ? "Estudiantes" : "Profesores"; }
    public async Task<IActionResult> Index(string? buscar)
    {
        Preparar(); ViewBag.Buscar = buscar;
        var consulta = Consulta().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(buscar)) consulta = consulta.Where(x => x.Correo.Contains(buscar)
            || (x.Estudiante != null && (x.Estudiante.Nombre.Contains(buscar) || x.Estudiante.Codigo.Contains(buscar)))
            || (x.Profesor != null && x.Profesor.Nombre.Contains(buscar)));
        return View("~/Views/Usuarios/Index.cshtml", await consulta.OrderBy(x => x.Correo).ToListAsync());
    }
    public IActionResult Create() { Preparar(); return View("~/Views/Usuarios/Edit.cshtml", new UsuarioViewModel { Rol = TipoPerfil ?? Roles.Estudiante }); }
    [HttpPost] public Task<IActionResult> Create(UsuarioViewModel model) { model.Id = 0; return Guardar(model); }
    public async Task<IActionResult> Edit(int id)
    {
        var usuario = await Consulta().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (usuario == null) return NotFound();
        Preparar();
        return View("~/Views/Usuarios/Edit.cshtml", new UsuarioViewModel { Id = id, Correo = usuario.Correo, Rol = usuario.Rol, Activo = usuario.Activo,
            Codigo = usuario.Estudiante?.Codigo, Nombre = usuario.Estudiante?.Nombre ?? usuario.Profesor?.Nombre,
            Carrera = usuario.Estudiante?.Carrera, Especialidad = usuario.Profesor?.Especialidad });
    }
    [HttpPost] public Task<IActionResult> Edit(int id, UsuarioViewModel model) { if (id != model.Id) return Task.FromResult<IActionResult>(BadRequest()); return Guardar(model); }
    private async Task<IActionResult> Guardar(UsuarioViewModel model)
    {
        Preparar();
        if (TipoPerfil != null && model.Rol != TipoPerfil) return BadRequest();
        var usuario = model.Id == 0 ? new Usuario() : await Consulta().SingleOrDefaultAsync(x => x.Id == model.Id);
        if (usuario == null) return NotFound();
        model.Correo = (model.Correo ?? "").Trim().ToLowerInvariant();
        model.Codigo = model.Codigo?.Trim();
        if (await db.Usuarios.AnyAsync(x => x.Correo == model.Correo && x.Id != model.Id)) ModelState.AddModelError(nameof(model.Correo), "El correo ya está registrado.");
        if (model.Rol == Roles.Estudiante && await db.Estudiantes.AnyAsync(x => x.Codigo == model.Codigo && x.UsuarioId != model.Id)) ModelState.AddModelError(nameof(model.Codigo), "El código ya está registrado.");
        if (model.Id != 0 && usuario.Rol != model.Rol && (usuario.Estudiante != null || usuario.Profesor != null))
            ModelState.AddModelError(nameof(model.Rol), "Una cuenta con perfil debe conservar su rol. Desactívala y crea la cuenta correspondiente.");
        if (model.Id != 0 && usuario.Rol == Roles.Administrador && (!model.Activo || model.Rol != Roles.Administrador)
            && (usuario.Id == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) || !await db.Usuarios.AnyAsync(x => x.Id != usuario.Id && x.Activo && x.Rol == Roles.Administrador)))
            ModelState.AddModelError("", "No puedes desactivar tu propia cuenta ni quitar el último administrador activo.");
        if (!ModelState.IsValid) return View("~/Views/Usuarios/Edit.cshtml", model);
        usuario.Correo = model.Correo; usuario.Rol = model.Rol; usuario.Activo = model.Activo;
        if (!string.IsNullOrWhiteSpace(model.Password)) usuario.PasswordHash = new PasswordHasher<Usuario>().HashPassword(usuario, model.Password);
        if (model.Rol == Roles.Estudiante)
        {
            usuario.Estudiante ??= new Estudiante();
            usuario.Estudiante.Codigo = model.Codigo!.Trim(); usuario.Estudiante.Nombre = model.Nombre!.Trim(); usuario.Estudiante.Carrera = model.Carrera!.Trim();
        }
        if (model.Rol == Roles.Profesor)
        {
            usuario.Profesor ??= new Profesor();
            usuario.Profesor.Nombre = model.Nombre!.Trim(); usuario.Profesor.Especialidad = model.Especialidad!.Trim();
        }
        if (model.Id == 0) db.Usuarios.Add(usuario);
        await db.SaveChangesAsync(); TempData["Exito"] = "Cuenta y perfil guardados correctamente.";
        return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Details(int id)
    {
        var usuario = await Consulta().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (usuario == null) return NotFound(); Preparar();
        ViewBag.Asignaciones = await db.Tribunales.AsNoTracking().Include(x => x.Proyecto).Include(x => x.Evaluacion).Where(x => x.Profesor.UsuarioId == id).ToListAsync();
        return View("~/Views/Usuarios/Details.cshtml", usuario);
    }
    [HttpPost] public async Task<IActionResult> Desactivar(int id)
    {
        var usuario = await Consulta().SingleOrDefaultAsync(x => x.Id == id);
        if (usuario == null) return NotFound();
        if (usuario.Id == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!) ||
            (usuario.Rol == Roles.Administrador && !await db.Usuarios.AnyAsync(x => x.Id != id && x.Activo && x.Rol == Roles.Administrador)))
            TempData["Error"] = "No puedes desactivar tu propia cuenta ni el último administrador activo.";
        else { usuario.Activo = false; await db.SaveChangesAsync(); TempData["Exito"] = "Cuenta desactivada. Se conserva su historial."; }
        return RedirectToAction(nameof(Index));
    }
    [HttpPost] public async Task<IActionResult> Delete(int id)
    {
        var usuario = await Consulta().SingleOrDefaultAsync(x => x.Id == id);
        if (usuario == null) return NotFound();
        var propio = usuario.Id == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var ultimoAdmin = usuario.Rol == Roles.Administrador && !await db.Usuarios.AnyAsync(x => x.Id != id && x.Activo && x.Rol == Roles.Administrador);
        var tieneHistorial = await db.GrupoEstudiantes.AnyAsync(x => x.Estudiante.UsuarioId == id)
            || await db.Grupos.AnyAsync(x => x.ResponsableEstudiante.UsuarioId == id)
            || await db.Tribunales.AnyAsync(x => x.Profesor.UsuarioId == id)
            || await db.Ferias.AnyAsync(x => x.PublicadoPorUsuarioId == id);
        if (propio || ultimoAdmin || tieneHistorial) TempData["Error"] = "No se puede eliminar esta cuenta porque es propia, es el último administrador o tiene historial. Puedes desactivar las cuentas con historial.";
        else
        {
            if (usuario.Estudiante != null) db.Estudiantes.Remove(usuario.Estudiante);
            if (usuario.Profesor != null) db.Profesores.Remove(usuario.Profesor);
            db.Usuarios.Remove(usuario); await db.SaveChangesAsync(); TempData["Exito"] = "Cuenta sin historial eliminada.";
        }
        return RedirectToAction(nameof(Index));
    }
}
