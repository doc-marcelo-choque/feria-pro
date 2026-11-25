using System.Security.Claims;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.Services;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
[Authorize]
public class ProyectosController(AppDbContext db, ArchivoService archivos) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IQueryable<Proyecto> Visibles() => db.Proyectos.Where(p => User.IsInRole(Roles.Administrador)
        || (User.IsInRole(Roles.Profesor) && p.Tribunales.Any(t => t.Profesor.UsuarioId == UsuarioId))
        || (User.IsInRole(Roles.Estudiante) && p.Grupo.GrupoEstudiantes.Any(g => g.Estudiante.UsuarioId == UsuarioId)));
    private IQueryable<Proyecto> Consulta() => Visibles().Include(x => x.Feria).Include(x => x.Grupo).ThenInclude(x => x.ResponsableEstudiante)
        .Include(x => x.Grupo).ThenInclude(x => x.GrupoEstudiantes).ThenInclude(x => x.Estudiante);
    private bool PuedeEditar(Proyecto p) => p.Feria.Estado == EstadosFeria.Abierta && (User.IsInRole(Roles.Administrador) || p.Grupo.ResponsableEstudiante.UsuarioId == UsuarioId);
    private async Task Preparar(ProyectoViewModel? model = null)
    {
        ViewBag.Ferias = new SelectList(await db.Ferias.AsNoTracking().Where(x => x.Estado == EstadosFeria.Abierta).OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre", model?.FeriaId);
        ViewBag.Grupos = new SelectList(await db.Grupos.AsNoTracking().Where(x => User.IsInRole(Roles.Administrador) || x.ResponsableEstudiante.UsuarioId == UsuarioId).OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre", model?.GrupoId);
    }
    public async Task<IActionResult> Index(int? feriaId, string? buscar)
    {
        var consulta = Consulta().AsNoTracking();
        if (feriaId.HasValue) consulta = consulta.Where(x => x.FeriaId == feriaId);
        if (!string.IsNullOrWhiteSpace(buscar)) consulta = consulta.Where(x => x.Titulo.Contains(buscar));
        ViewBag.Buscar = buscar; ViewBag.FeriaId = feriaId;
        ViewBag.Ferias = new SelectList(await db.Ferias.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre", feriaId);
        return View(await consulta.OrderBy(x => x.Titulo).ToListAsync());
    }
    public async Task<IActionResult> Details(int id)
    {
        var proyecto = await Consulta().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (proyecto == null) return NotFound();
        ViewBag.PuedeEditar = PuedeEditar(proyecto);
        return View(proyecto);
    }
    [Authorize(Roles = Roles.Estudiante + "," + Roles.Administrador)] public async Task<IActionResult> Create()
    { await Preparar(); return View("Edit", new ProyectoViewModel()); }
    [HttpPost, Authorize(Roles = Roles.Estudiante + "," + Roles.Administrador), RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Create(ProyectoViewModel model) { model.Id = 0; return Guardar(model); }
    [Authorize(Roles = Roles.Estudiante + "," + Roles.Administrador)] public async Task<IActionResult> Edit(int id)
    {
        var p = await Consulta().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound(); if (!PuedeEditar(p)) return Forbid();
        var model = new ProyectoViewModel { Id = id, Titulo = p.Titulo, Descripcion = p.Descripcion, FeriaId = p.FeriaId, GrupoId = p.GrupoId, PdfUrl = p.PdfUrl };
        await Preparar(model); return View(model);
    }
    [HttpPost, Authorize(Roles = Roles.Estudiante + "," + Roles.Administrador), RequestSizeLimit(6 * 1024 * 1024)]
    public Task<IActionResult> Edit(int id, ProyectoViewModel model) => id != model.Id ? Task.FromResult<IActionResult>(BadRequest()) : Guardar(model);
    private async Task<IActionResult> Guardar(ProyectoViewModel model)
    {
        var proyecto = model.Id == 0 ? new Proyecto() : await Consulta().SingleOrDefaultAsync(x => x.Id == model.Id);
        if (proyecto == null) return NotFound();
        if (model.Id != 0 && !PuedeEditar(proyecto)) return Forbid();
        if (!ModelState.IsValid) { await Preparar(model); return View("Edit", model); }
        var feria = await db.Ferias.SingleOrDefaultAsync(x => x.Id == model.FeriaId);
        var grupo = await db.Grupos.Include(x => x.ResponsableEstudiante).Include(x => x.GrupoEstudiantes).ThenInclude(x => x.Estudiante).ThenInclude(x => x.Usuario).SingleOrDefaultAsync(x => x.Id == model.GrupoId);
        if (grupo == null || (!User.IsInRole(Roles.Administrador) && grupo.ResponsableEstudiante.UsuarioId != UsuarioId)) return Forbid();
        if (feria == null || feria.Estado != EstadosFeria.Abierta) ModelState.AddModelError("FeriaId", "Selecciona una feria abierta.");
        if (grupo.GrupoEstudiantes.Count == 0 || grupo.GrupoEstudiantes.Any(x => !x.Estudiante.Usuario.Activo)) ModelState.AddModelError("GrupoId", "Todos los integrantes del grupo deben tener cuentas activas.");
        if (await db.Proyectos.AnyAsync(x => x.Id != model.Id && x.GrupoId == model.GrupoId && x.FeriaId == model.FeriaId)) ModelState.AddModelError("GrupoId", "El grupo ya tiene un proyecto en esta feria.");
        var integrantes = grupo.GrupoEstudiantes.Select(x => x.EstudianteId).ToList();
        if (await db.Proyectos.AnyAsync(x => x.Id != model.Id && x.GrupoId != model.GrupoId && x.FeriaId == model.FeriaId && x.Grupo.GrupoEstudiantes.Any(g => integrantes.Contains(g.EstudianteId))))
            ModelState.AddModelError("GrupoId", "Un integrante ya participa con otro grupo en esta feria.");
        if (model.Id != 0 && (proyecto.FeriaId != model.FeriaId || proyecto.GrupoId != model.GrupoId) && await db.Tribunales.AnyAsync(x => x.ProyectoId == model.Id))
            ModelState.AddModelError("", "Retira las asignaciones sin evaluación antes de cambiar la feria o el grupo.");
        string? nuevaFoto = null;
        if (ModelState.IsValid && model.Fotografia != null)
        {
            try { nuevaFoto = await archivos.GuardarAsync(model.Fotografia); }
            catch (ArgumentException error) { ModelState.AddModelError("Fotografia", error.Message); }
        }
        if (!ModelState.IsValid) { await Preparar(model); return View("Edit", model); }
        proyecto.FeriaId = model.FeriaId; proyecto.GrupoId = model.GrupoId; proyecto.Titulo = model.Titulo.Trim(); proyecto.Descripcion = model.Descripcion.Trim(); proyecto.PdfUrl = model.PdfUrl;
        if (nuevaFoto != null) proyecto.FotoUrl = nuevaFoto;
        if (model.Id == 0) db.Proyectos.Add(proyecto);
        try { await db.SaveChangesAsync(); }
        catch { archivos.Eliminar(nuevaFoto); throw; }
        TempData["Exito"] = "Proyecto guardado correctamente."; return RedirectToAction(nameof(Details), new { id = proyecto.Id });
    }
    [HttpPost, Authorize(Roles = Roles.Estudiante + "," + Roles.Administrador)] public async Task<IActionResult> Delete(int id)
    {
        var proyecto = await Consulta().SingleOrDefaultAsync(x => x.Id == id);
        if (proyecto == null) return NotFound(); if (!PuedeEditar(proyecto)) return Forbid();
        if (await db.Tribunales.AnyAsync(x => x.ProyectoId == id)) { TempData["Error"] = "Retira las asignaciones antes de eliminar el proyecto."; return RedirectToAction(nameof(Details), new { id }); }
        db.Proyectos.Remove(proyecto); await db.SaveChangesAsync(); TempData["Exito"] = "Proyecto eliminado."; return RedirectToAction(nameof(Index));
    }
}
