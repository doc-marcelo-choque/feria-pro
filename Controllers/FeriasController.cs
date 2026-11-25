using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
[Authorize]
public class FeriasController(AppDbContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.Ferias.AsNoTracking().OrderByDescending(x => x.FechaInicio).ToListAsync());
    public async Task<IActionResult> Details(int id)
    {
        var feria = await db.Ferias.AsNoTracking().Include(x => x.Proyectos).ThenInclude(x => x.Grupo).SingleOrDefaultAsync(x => x.Id == id);
        if (feria == null) return NotFound();
        if (!User.IsInRole(Roles.Administrador)) feria.Proyectos = feria.Proyectos.Where(x => false).ToList();
        return View(feria);
    }
    [Authorize(Roles = Roles.Administrador)] public IActionResult Create() => View("Edit", new FeriaViewModel());
    [HttpPost, Authorize(Roles = Roles.Administrador)] public Task<IActionResult> Create(FeriaViewModel model) { model.Id = 0; return Guardar(model); }
    [Authorize(Roles = Roles.Administrador)] public async Task<IActionResult> Edit(int id)
    {
        var f = await db.Ferias.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return f == null ? NotFound() : View(new FeriaViewModel { Id = id, Nombre = f.Nombre, FechaInicio = f.FechaInicio, FechaFin = f.FechaFin, Estado = f.Estado });
    }
    [HttpPost, Authorize(Roles = Roles.Administrador)] public Task<IActionResult> Edit(int id, FeriaViewModel model) => id != model.Id ? Task.FromResult<IActionResult>(BadRequest()) : Guardar(model);
    private async Task<IActionResult> Guardar(FeriaViewModel model)
    {
        var feria = model.Id == 0 ? new Feria() : await db.Ferias.Include(x => x.Proyectos).ThenInclude(x => x.Tribunales).ThenInclude(x => x.Evaluacion).SingleOrDefaultAsync(x => x.Id == model.Id);
        if (feria == null) return NotFound();
        if (feria.Estado == EstadosFeria.Finalizada) ModelState.AddModelError("", "Una feria finalizada es inmutable para conservar sus resultados.");
        if (model.Id == 0 && model.Estado != EstadosFeria.Abierta) ModelState.AddModelError("Estado", "Las ferias nuevas comienzan abiertas.");
        if (feria.Estado == EstadosFeria.EnEvaluacion && model.Estado == EstadosFeria.Abierta) ModelState.AddModelError("Estado", "No se pueden reabrir inscripciones después de iniciar la evaluación.");
        if (model.Estado == EstadosFeria.EnEvaluacion && (feria.Proyectos.Any(x => x.Tribunales.Count != 3)
            || await db.Tribunales.AnyAsync(x => x.Proyecto.FeriaId == model.Id && !x.Profesor.Usuario.Activo)))
            ModelState.AddModelError("Estado", "Cada proyecto debe tener tres profesores activos antes de iniciar la evaluación.");
        if (model.Estado == EstadosFeria.Finalizada && (feria.Estado != EstadosFeria.EnEvaluacion || feria.Proyectos.Any(x => x.Tribunales.Count != 3 || x.Tribunales.Any(t => t.Evaluacion == null))))
            ModelState.AddModelError("Estado", "Solo se puede finalizar desde evaluación con tres evaluaciones completas por proyecto.");
        if (!ModelState.IsValid) return View("Edit", model);
        feria.Nombre = model.Nombre.Trim(); feria.FechaInicio = model.FechaInicio; feria.FechaFin = model.FechaFin; feria.Estado = model.Estado;
        foreach (var proyecto in feria.Proyectos) proyecto.Estado = model.Estado == EstadosFeria.Abierta ? "Registrado" : proyecto.Tribunales.All(x => x.Evaluacion != null) ? "Evaluado" : "EnEvaluacion";
        if (model.Id == 0) db.Ferias.Add(feria);
        await db.SaveChangesAsync(); TempData["Exito"] = "Feria guardada correctamente."; return RedirectToAction(nameof(Index));
    }
    [HttpPost, Authorize(Roles = Roles.Administrador)] public async Task<IActionResult> Delete(int id)
    {
        var feria = await db.Ferias.SingleOrDefaultAsync(x => x.Id == id);
        if (feria == null) return NotFound();
        if (feria.Estado != EstadosFeria.Abierta || await db.Proyectos.AnyAsync(x => x.FeriaId == id)) TempData["Error"] = "Solo se puede eliminar una feria abierta sin proyectos.";
        else { db.Ferias.Remove(feria); await db.SaveChangesAsync(); TempData["Exito"] = "Feria eliminada."; }
        return RedirectToAction(nameof(Index));
    }
}
