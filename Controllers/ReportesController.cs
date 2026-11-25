using System.Globalization;
using System.Text;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.Services;
using FeriaPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Controllers;
[Authorize(Roles = Roles.Administrador)]
public class ReportesController(AppDbContext db, RankingService ranking) : Controller
{
    public IActionResult Index() => View();
    public async Task<IActionResult> Proyectos(int? feriaId, bool csv = false)
    {
        var consulta = db.Proyectos.AsNoTracking().Include(x => x.Feria).Include(x => x.Grupo).ThenInclude(x => x.GrupoEstudiantes).ThenInclude(x => x.Estudiante).AsQueryable();
        if (feriaId.HasValue) consulta = consulta.Where(x => x.FeriaId == feriaId);
        var proyectos = await consulta.OrderBy(x => x.Feria.Nombre).ThenBy(x => x.Titulo).ToListAsync();
        return await Mostrar(new ReporteViewModel { Titulo = "Proyectos por feria", Columnas = ["Feria", "Proyecto", "Grupo", "Integrantes", "Estado"],
            Filas = proyectos.Select(x => new[] { x.Feria.Nombre, x.Titulo, x.Grupo.Nombre, string.Join(", ", x.Grupo.GrupoEstudiantes.Select(g => g.Estudiante.Nombre)), x.Estado }).ToList() }, feriaId, csv);
    }
    public async Task<IActionResult> Evaluaciones(int? feriaId, bool csv = false)
    {
        var consulta = db.Evaluaciones.AsNoTracking().Include(x => x.Tribunal).ThenInclude(x => x.Proyecto).ThenInclude(x => x.Feria).Include(x => x.Tribunal).ThenInclude(x => x.Profesor).AsQueryable();
        if (feriaId.HasValue) consulta = consulta.Where(x => x.Tribunal.Proyecto.FeriaId == feriaId);
        var evaluaciones = await consulta.OrderBy(x => x.Tribunal.Proyecto.Titulo).ToListAsync();
        return await Mostrar(new ReporteViewModel { Titulo = "Evaluaciones", Columnas = ["Feria", "Proyecto", "Profesor", "Innovación", "Desarrollo técnico", "Presentación", "Nota"],
            Filas = evaluaciones.Select(x => new[] { x.Tribunal.Proyecto.Feria.Nombre, x.Tribunal.Proyecto.Titulo, x.Tribunal.Profesor.Nombre, Numero(x.Innovacion), Numero(x.DesarrolloTecnico), Numero(x.Presentacion), Numero(x.Nota) }).ToList() }, feriaId, csv);
    }
    public async Task<IActionResult> Profesores(int? feriaId, bool csv = false)
    {
        var profesores = await db.Profesores.AsNoTracking().OrderBy(x => x.Nombre).Select(x => new { x.Nombre,
            Asignados = x.Tribunales.Count(t => !feriaId.HasValue || t.Proyecto.FeriaId == feriaId),
            Evaluados = x.Tribunales.Count(t => (!feriaId.HasValue || t.Proyecto.FeriaId == feriaId) && t.Evaluacion != null) }).ToListAsync();
        return await Mostrar(new ReporteViewModel { Titulo = "Profesores", Columnas = ["Profesor", "Proyectos asignados", "Evaluaciones realizadas", "Pendientes"],
            Filas = profesores.Select(x => new[] { x.Nombre, x.Asignados.ToString(), x.Evaluados.ToString(), (x.Asignados - x.Evaluados).ToString() }).ToList() }, feriaId, csv);
    }
    public async Task<IActionResult> Ranking(int? feriaId, bool csv = false)
    {
        var ferias = await db.Ferias.AsNoTracking().Where(x => x.Estado == EstadosFeria.Finalizada && (!feriaId.HasValue || x.Id == feriaId)).ToListAsync();
        var model = new ReporteViewModel { Titulo = "Ranking por feria", Columnas = ["Feria", "Posición", "Proyecto", "Grupo", "Nota final", "Publicación"] };
        foreach (var feria in ferias)
        {
            var resultado = (await ranking.CalcularAsync(feria.Id))!;
            model.Filas.AddRange(resultado.Resultados.Select(x => new[] { feria.Nombre, x.Posicion + (x.Empatado ? " (empate)" : ""), x.Titulo, x.Grupo, Numero(x.NotaFinal), feria.ResultadosPublicados ? "Oficial" : "Sin publicar" }));
        }
        return await Mostrar(model, feriaId, csv);
    }
    private static string Numero(decimal nota) => nota.ToString("F2", CultureInfo.InvariantCulture);
    private async Task<IActionResult> Mostrar(ReporteViewModel model, int? feriaId, bool csv)
    {
        if (csv)
        {
            static string Celda(string valor)
            {
                if (valor.Length > 0 && ("=+-@\t\r\n".Contains(valor[0]) || "=+-@".Contains(valor.TrimStart().FirstOrDefault()))) valor = "'" + valor;
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }
            var contenido = new StringBuilder(); contenido.AppendLine(string.Join(",", model.Columnas.Select(Celda)));
            foreach (var fila in model.Filas) contenido.AppendLine(string.Join(",", fila.Select(Celda)));
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(contenido.ToString())).ToArray(), "text/csv; charset=utf-8", $"FeriaPro-{ControllerContext.ActionDescriptor.ActionName}.csv");
        }
        model.FeriaId = feriaId; model.Ferias = await db.Ferias.AsNoTracking().OrderBy(x => x.Nombre).ToListAsync();
        return View("Reporte", model);
    }
}
