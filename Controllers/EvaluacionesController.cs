using System.Security.Claims;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
[Authorize(Roles = Roles.Profesor + "," + Roles.Administrador)]
public class EvaluacionesController(AppDbContext db) : Controller
{
    private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    public async Task<IActionResult> Index()
    {
        var consulta = db.Tribunales.AsNoTracking().Include(x => x.Profesor).Include(x => x.Proyecto).ThenInclude(x => x.Feria).Include(x => x.Evaluacion).AsQueryable();
        if (!User.IsInRole(Roles.Administrador)) consulta = consulta.Where(x => x.Profesor.UsuarioId == UsuarioId);
        return View(await consulta.OrderBy(x => x.Proyecto.Titulo).ToListAsync());
    }
    private Task<Tribunal?> Asignacion(int id) => db.Tribunales.Include(x => x.Evaluacion).Include(x => x.Proyecto).ThenInclude(x => x.Feria)
        .Include(x => x.Proyecto).ThenInclude(x => x.Grupo).ThenInclude(x => x.GrupoEstudiantes).ThenInclude(x => x.Estudiante)
        .SingleOrDefaultAsync(x => x.Id == id && x.Profesor.UsuarioId == UsuarioId);
    [Authorize(Roles = Roles.Profesor)] public async Task<IActionResult> Edit(int id)
    {
        var tribunal = await Asignacion(id);
        if (tribunal == null) return NotFound();
        if (tribunal.Proyecto.Feria.Estado != EstadosFeria.EnEvaluacion) return Forbid();
        return View(new EvaluacionViewModel { TribunalId = id, Proyecto = tribunal.Proyecto, Innovacion = tribunal.Evaluacion?.Innovacion,
            DesarrolloTecnico = tribunal.Evaluacion?.DesarrolloTecnico, Presentacion = tribunal.Evaluacion?.Presentacion, Observaciones = tribunal.Evaluacion?.Observaciones });
    }
    [HttpPost, Authorize(Roles = Roles.Profesor)] public async Task<IActionResult> Edit(int id, EvaluacionViewModel model)
    {
        if (id != model.TribunalId) return BadRequest();
        var tribunal = await Asignacion(id);
        if (tribunal == null) return NotFound();
        if (tribunal.Proyecto.Feria.Estado != EstadosFeria.EnEvaluacion) return Forbid();
        if (await db.Tribunales.CountAsync(x => x.ProyectoId == tribunal.ProyectoId) != 3) ModelState.AddModelError("", "El administrador debe completar las tres asignaciones antes de evaluar.");
        if (new[] { model.Innovacion, model.DesarrolloTecnico, model.Presentacion }.Any(x => x.HasValue && decimal.Round(x.Value, 2) != x.Value))
            ModelState.AddModelError("", "Utiliza como máximo dos decimales por criterio.");
        model.Proyecto = tribunal.Proyecto;
        if (!ModelState.IsValid) return View(model);
        tribunal.Evaluacion ??= new Evaluacion { TribunalId = id };
        tribunal.Evaluacion.Innovacion = model.Innovacion!.Value; tribunal.Evaluacion.DesarrolloTecnico = model.DesarrolloTecnico!.Value;
        tribunal.Evaluacion.Presentacion = model.Presentacion!.Value; tribunal.Evaluacion.Observaciones = model.Observaciones;
        tribunal.Evaluacion.FechaEvaluacion = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var completadas = await db.Evaluaciones.CountAsync(x => x.Tribunal.ProyectoId == tribunal.ProyectoId);
        tribunal.Proyecto.Estado = completadas == 3 ? "Evaluado" : "EnEvaluacion";
        await db.SaveChangesAsync(); TempData["Exito"] = $"Evaluación guardada. Nota: {tribunal.Evaluacion.Nota:N2}.";
        return RedirectToAction(nameof(Index));
    }
}
