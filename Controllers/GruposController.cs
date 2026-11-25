using System.Security.Claims;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
[Authorize(Roles = Roles.Estudiante + "," + Roles.Administrador)]
public class GruposController(AppDbContext db) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<Grupo> Consulta() => db.Grupos.Include(x => x.ResponsableEstudiante).Include(x => x.GrupoEstudiantes).ThenInclude(x => x.Estudiante).Include(x => x.Proyectos).ThenInclude(x => x.Feria);
    private bool PuedeAdministrar(Grupo grupo) => User.IsInRole(Roles.Estudiante) && grupo.ResponsableEstudiante.UsuarioId == UsuarioId;
    private bool Bloqueado(Grupo grupo) => grupo.Proyectos.Any(x => x.Feria.Estado != EstadosFeria.Abierta);
    public async Task<IActionResult> Index()
    {
        var grupos = Consulta().AsNoTracking();
        if (!User.IsInRole(Roles.Administrador)) grupos = grupos.Where(x => x.GrupoEstudiantes.Any(g => g.Estudiante.UsuarioId == UsuarioId));
        return View(await grupos.OrderBy(x => x.Nombre).ToListAsync());
    }
    [Authorize(Roles = Roles.Estudiante)] public IActionResult Create() => View("Edit", new GrupoViewModel());
    [HttpPost, Authorize(Roles = Roles.Estudiante)] public async Task<IActionResult> Create(GrupoViewModel model)
    {
        model.Id = 0;
        var estudiante = await db.Estudiantes.SingleOrDefaultAsync(x => x.UsuarioId == UsuarioId);
        if (estudiante == null) return Forbid();
        if (!ModelState.IsValid) return View("Edit", model);
        var grupo = new Grupo { Nombre = model.Nombre.Trim(), ResponsableEstudianteId = estudiante.Id };
        grupo.GrupoEstudiantes.Add(new GrupoEstudiante { EstudianteId = estudiante.Id });
        db.Grupos.Add(grupo); await db.SaveChangesAsync(); TempData["Exito"] = "Grupo creado. Eres su responsable y primer integrante.";
        return RedirectToAction(nameof(Details), new { id = grupo.Id });
    }
    public async Task<IActionResult> Details(int id)
    {
        var grupo = await Consulta().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (grupo == null) return NotFound();
        if (!User.IsInRole(Roles.Administrador) && !grupo.GrupoEstudiantes.Any(x => x.Estudiante.UsuarioId == UsuarioId)) return Forbid();
        ViewBag.PuedeAdministrar = PuedeAdministrar(grupo) && !Bloqueado(grupo);
        ViewBag.Estudiantes = await db.Estudiantes.AsNoTracking().Where(x => x.Usuario.Activo && !x.GrupoEstudiantes.Any(g => g.GrupoId == id)).OrderBy(x => x.Nombre).ToListAsync();
        return View(grupo);
    }
    [Authorize(Roles = Roles.Estudiante)] public async Task<IActionResult> Edit(int id)
    {
        var grupo = await Consulta().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (grupo == null) return NotFound(); if (!PuedeAdministrar(grupo) || Bloqueado(grupo)) return Forbid();
        return View(new GrupoViewModel { Id = id, Nombre = grupo.Nombre });
    }
    [HttpPost, Authorize(Roles = Roles.Estudiante)] public async Task<IActionResult> Edit(int id, GrupoViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var grupo = await Consulta().SingleOrDefaultAsync(x => x.Id == id);
        if (grupo == null) return NotFound(); if (!PuedeAdministrar(grupo) || Bloqueado(grupo)) return Forbid();
        if (!ModelState.IsValid) return View(model);
        grupo.Nombre = model.Nombre.Trim(); await db.SaveChangesAsync(); TempData["Exito"] = "Grupo actualizado."; return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Roles = Roles.Estudiante)] public async Task<IActionResult> Agregar(int id, IntegranteViewModel model)
    {
        var grupo = await Consulta().SingleOrDefaultAsync(x => x.Id == id);
        if (grupo == null) return NotFound(); if (!PuedeAdministrar(grupo) || Bloqueado(grupo)) return Forbid();
        var estudiante = await db.Estudiantes.AsNoTracking().SingleOrDefaultAsync(x => x.Id == model.EstudianteId && x.Usuario.Activo);
        if (!ModelState.IsValid || estudiante == null) return BadRequest();
        var ferias = grupo.Proyectos.Select(x => x.FeriaId).ToList();
        if (grupo.GrupoEstudiantes.Any(x => x.EstudianteId == estudiante.Id)) TempData["Error"] = "El estudiante ya pertenece al grupo.";
        else if (await db.GrupoEstudiantes.AnyAsync(x => x.EstudianteId == estudiante.Id && x.GrupoId != id && x.Grupo.Proyectos.Any(p => ferias.Contains(p.FeriaId))))
            TempData["Error"] = "El estudiante ya participa en otro proyecto de la misma feria.";
        else { db.GrupoEstudiantes.Add(new GrupoEstudiante { GrupoId = id, EstudianteId = estudiante.Id }); await db.SaveChangesAsync(); TempData["Exito"] = "Integrante agregado."; }
        return RedirectToAction(nameof(Details), new { id });
    }
    [HttpPost, Authorize(Roles = Roles.Estudiante)] public async Task<IActionResult> Retirar(int id, IntegranteViewModel model)
    {
        var grupo = await Consulta().SingleOrDefaultAsync(x => x.Id == id);
        if (grupo == null) return NotFound(); if (!PuedeAdministrar(grupo) || Bloqueado(grupo)) return Forbid();
        if (!ModelState.IsValid) return BadRequest();
        var integrante = grupo.GrupoEstudiantes.SingleOrDefault(x => x.EstudianteId == model.EstudianteId);
        if (integrante == null) return NotFound();
        if (grupo.ResponsableEstudianteId == model.EstudianteId || grupo.GrupoEstudiantes.Count <= 1) TempData["Error"] = "El responsable debe permanecer en el grupo.";
        else { db.GrupoEstudiantes.Remove(integrante); await db.SaveChangesAsync(); TempData["Exito"] = "Integrante retirado."; }
        return RedirectToAction(nameof(Details), new { id });
    }
}
