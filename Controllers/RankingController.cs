using System.Security.Claims;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.Services;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
public class RankingController(AppDbContext db, RankingService ranking) : Controller
{
    [AllowAnonymous] public async Task<IActionResult> Index()
    {
        var consulta = db.Ferias.AsNoTracking().Where(x => x.ResultadosPublicados || (User.IsInRole(Roles.Administrador) && x.Estado == EstadosFeria.Finalizada));
        return View(await consulta.OrderByDescending(x => x.FechaFin).ToListAsync());
    }
    [AllowAnonymous] public async Task<IActionResult> Details(int id)
    {
        var feria = await db.Ferias.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (feria == null || feria.Estado != EstadosFeria.Finalizada || (!feria.ResultadosPublicados && !User.IsInRole(Roles.Administrador))) return NotFound();
        return View(await ranking.CalcularAsync(id));
    }
    [HttpPost, Authorize(Roles = Roles.Administrador)] public async Task<IActionResult> Publicar(int id, PublicarRankingViewModel model)
    {
        var feria = await db.Ferias.SingleOrDefaultAsync(x => x.Id == id);
        if (feria == null) return NotFound(); if (feria.Estado != EstadosFeria.Finalizada || feria.ResultadosPublicados) return Forbid();
        var resultado = (await ranking.CalcularAsync(id))!;
        if (resultado.Resultados.Count == 0 || resultado.Resultados.Count != resultado.Participantes) ModelState.AddModelError("", "Todos los proyectos deben tener sus tres evaluaciones completas.");
        if (resultado.EmpatesPendientes)
        {
            var orden = RankingService.LeerOrden(model.OrdenDesempate);
            if (orden.Length != resultado.Resultados.Count || orden.Distinct().Count() != orden.Length || resultado.Resultados.Any(x => !orden.Contains(x.ProyectoId))) ModelState.AddModelError("", "Indica una lista completa de IDs de proyectos, sin repeticiones, para resolver los empates.");
            if (string.IsNullOrWhiteSpace(model.ResolucionEmpate) || model.ResolucionEmpate.Trim().Length < 20) ModelState.AddModelError("", "Documenta la resolución del empate con al menos 20 caracteres.");
        }
        if (!ModelState.IsValid) { TempData["Error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage)); return RedirectToAction(nameof(Details), new { id }); }
        if (resultado.EmpatesPendientes) { feria.OrdenDesempate = model.OrdenDesempate; feria.ResolucionEmpate = model.ResolucionEmpate!.Trim(); }
        feria.ResultadosPublicados = true; feria.PublicadoPorUsuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!); feria.FechaPublicacion = DateTime.UtcNow;
        await db.SaveChangesAsync(); TempData["Exito"] = "Resultados oficiales publicados."; return RedirectToAction(nameof(Details), new { id });
    }
}
