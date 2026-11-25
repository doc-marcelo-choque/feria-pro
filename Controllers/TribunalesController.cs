using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
[Authorize(Roles = Roles.Administrador)]
public class TribunalesController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index(int? proyectoId)
    {
        ViewBag.Proyectos = new SelectList(await db.Proyectos.AsNoTracking().OrderBy(x => x.Titulo).ToListAsync(), "Id", "Titulo", proyectoId);
        ViewBag.Profesores = new SelectList(await db.Profesores.AsNoTracking().Where(x => x.Usuario.Activo).OrderBy(x => x.Nombre).ToListAsync(), "Id", "Nombre");
        ViewBag.ProyectoId = proyectoId;
        var consulta = db.Tribunales.AsNoTracking().Include(x => x.Proyecto).ThenInclude(x => x.Feria).Include(x => x.Profesor).Include(x => x.Evaluacion).AsQueryable();
        if (proyectoId.HasValue) consulta = consulta.Where(x => x.ProyectoId == proyectoId);
        return View(await consulta.OrderBy(x => x.Proyecto.Titulo).ThenBy(x => x.Profesor.Nombre).ToListAsync());
    }
    [HttpPost] public async Task<IActionResult> Asignar(TribunalViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest();
        var proyecto = await db.Proyectos.Include(x => x.Feria).Include(x => x.Tribunales).SingleOrDefaultAsync(x => x.Id == model.ProyectoId);
        var profesor = await db.Profesores.SingleOrDefaultAsync(x => x.Id == model.ProfesorId && x.Usuario.Activo);
        if (proyecto == null || profesor == null) return NotFound();
        if (proyecto.Feria.Estado == EstadosFeria.Finalizada) return Forbid();
        if (proyecto.Tribunales.Any(x => x.ProfesorId == model.ProfesorId)) TempData["Error"] = "El profesor ya está asignado a este proyecto.";
        else if (proyecto.Tribunales.Count >= 3) TempData["Error"] = "Cada proyecto tiene un máximo de tres evaluadores.";
        else { db.Tribunales.Add(new Tribunal { ProyectoId = proyecto.Id, ProfesorId = profesor.Id }); proyecto.Estado = proyecto.Feria.Estado == EstadosFeria.Abierta ? "Registrado" : "EnEvaluacion"; await db.SaveChangesAsync(); TempData["Exito"] = "Profesor asignado."; }
        return RedirectToAction(nameof(Index), new { proyectoId = model.ProyectoId });
    }
    [HttpPost] public async Task<IActionResult> Quitar(int id)
    {
        var tribunal = await db.Tribunales.Include(x => x.Evaluacion).Include(x => x.Proyecto).ThenInclude(x => x.Feria).SingleOrDefaultAsync(x => x.Id == id);
        if (tribunal == null) return NotFound();
        if (tribunal.Proyecto.Feria.Estado == EstadosFeria.Finalizada) return Forbid();
        if (tribunal.Evaluacion != null) TempData["Error"] = "La asignación tiene una evaluación y debe conservarse como historial.";
        else { db.Tribunales.Remove(tribunal); await db.SaveChangesAsync(); TempData["Exito"] = "Asignación retirada. Reemplázala antes de evaluar o finalizar."; }
        return RedirectToAction(nameof(Index), new { proyectoId = tribunal.ProyectoId });
    }
}
