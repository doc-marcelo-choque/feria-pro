using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using FeriaPro.Models;
using FeriaPro.Data;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace FeriaPro.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel();
        var usuarioId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (User.IsInRole(Roles.Administrador))
        {
            model.Estadisticas["Estudiantes"] = await _db.Estudiantes.CountAsync();
            model.Estadisticas["Profesores"] = await _db.Profesores.CountAsync();
            model.Estadisticas["Ferias"] = await _db.Ferias.CountAsync();
            model.Estadisticas["Grupos"] = await _db.Grupos.CountAsync();
            model.Estadisticas["Proyectos"] = await _db.Proyectos.CountAsync();
            var asignacionesPendientes = await _db.Tribunales.CountAsync(x => x.Evaluacion == null);
            var faltantes = await _db.Proyectos.Select(x => 3 - x.Tribunales.Count()).SumAsync();
            model.Estadisticas["Evaluaciones pendientes"] = asignacionesPendientes + faltantes;
            var ferias = await _db.Ferias.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new { x.Nombre, Cantidad = x.Proyectos.Count }).ToListAsync();
            model.Ferias = ferias.Select(x => x.Nombre).ToList(); model.ProyectosPorFeria = ferias.Select(x => x.Cantidad).ToList();
            var estados = await _db.Proyectos.AsNoTracking().GroupBy(x => x.Estado).Select(x => new { Estado = x.Key, Cantidad = x.Count() }).ToListAsync();
            model.Estados = estados.Select(x => x.Estado).ToList(); model.ProyectosPorEstado = estados.Select(x => x.Cantidad).ToList();
            model.Evaluados = await _db.Proyectos.CountAsync(x => x.Tribunales.Count() == 3 && x.Tribunales.All(t => t.Evaluacion != null));
            model.Pendientes = model.Estadisticas["Proyectos"] - model.Evaluados;
        }
        else if (User.IsInRole(Roles.Profesor))
        {
            var asignaciones = _db.Tribunales.Where(x => x.Profesor.UsuarioId == usuarioId);
            model.Estadisticas["Proyectos asignados"] = await asignaciones.CountAsync();
            model.Estadisticas["Evaluados"] = await asignaciones.CountAsync(x => x.Evaluacion != null);
            model.Estadisticas["Pendientes"] = await asignaciones.CountAsync(x => x.Evaluacion == null);
        }
        else
        {
            model.Estadisticas["Mis grupos"] = await _db.Grupos.CountAsync(x => x.GrupoEstudiantes.Any(g => g.Estudiante.UsuarioId == usuarioId));
            model.Estadisticas["Mis proyectos"] = await _db.Proyectos.CountAsync(x => x.Grupo.GrupoEstudiantes.Any(g => g.Estudiante.UsuarioId == usuarioId));
            model.FeriasDisponibles = await _db.Ferias.AsNoTracking().Where(x => x.Estado == EstadosFeria.Abierta).OrderBy(x => x.FechaInicio).ToListAsync();
            model.Estadisticas["Ferias disponibles"] = model.FeriasDisponibles.Count;
        }
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [Microsoft.AspNetCore.Mvc.IgnoreAntiforgeryToken]
    public IActionResult Estado(int id)
    {
        if (id is < 400 or > 599) id = 404;
        Response.StatusCode = id;
        return View(id);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
