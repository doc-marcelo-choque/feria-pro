using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.ViewModels;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Services;
public class RankingService(AppDbContext db)
{
    public async Task<RankingViewModel?> CalcularAsync(int feriaId)
    {
        var feria = await db.Ferias.AsNoTracking().SingleOrDefaultAsync(x => x.Id == feriaId);
        if (feria == null) return null;
        var modelo = new RankingViewModel { Feria = feria, Participantes = await db.Proyectos.CountAsync(x => x.FeriaId == feriaId) };
        if (feria.Estado != EstadosFeria.Finalizada) return modelo;
        var proyectos = await db.Proyectos.AsNoTracking().Include(x => x.Grupo).Include(x => x.Tribunales).ThenInclude(x => x.Evaluacion)
            .Where(x => x.FeriaId == feriaId && x.Tribunales.Count() == 3 && x.Tribunales.All(t => t.Evaluacion != null)).ToListAsync();
        var resultados = proyectos.Select(p => new ResultadoRanking { ProyectoId = p.Id, Titulo = p.Titulo, Grupo = p.Grupo.Nombre, FotoUrl = p.FotoUrl,
            Evaluaciones = p.Tribunales.Count, NotaFinal = p.Tribunales.Sum(t => t.Evaluacion!.Nota) / p.Tribunales.Count,
            DesarrolloTecnico = p.Tribunales.Average(t => t.Evaluacion!.DesarrolloTecnico), Innovacion = p.Tribunales.Average(t => t.Evaluacion!.Innovacion), Presentacion = p.Tribunales.Average(t => t.Evaluacion!.Presentacion) }).ToList();
        var orden = LeerOrden(feria.OrdenDesempate);
        var resuelto = orden.Length == resultados.Count && orden.Distinct().Count() == orden.Length && resultados.All(x => orden.Contains(x.ProyectoId)) && !string.IsNullOrWhiteSpace(feria.ResolucionEmpate);
        modelo.Resultados = Ordenar(resultados, resuelto ? orden : []);
        return modelo;
    }
    public static int[] LeerOrden(string? texto) => (texto ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => int.TryParse(x, out var id) ? id : -1).ToArray();
    public static List<ResultadoRanking> Ordenar(IEnumerable<ResultadoRanking> resultados, int[] orden)
    {
        var lista = resultados.OrderByDescending(x => x.NotaFinal).ThenByDescending(x => x.DesarrolloTecnico)
            .ThenByDescending(x => x.Innovacion).ThenByDescending(x => x.Presentacion)
            .ThenBy(x => Array.IndexOf(orden, x.ProyectoId)).ToList();
        var grupos = lista.GroupBy(x => (x.NotaFinal, x.DesarrolloTecnico, x.Innovacion, x.Presentacion));
        var posicion = 1;
        foreach (var grupo in grupos)
        {
            var empate = grupo.Count() > 1 && orden.Length == 0;
            var inicio = posicion;
            foreach (var resultado in grupo) { resultado.Empatado = empate; resultado.Posicion = empate ? inicio : posicion; posicion++; }
        }
        return lista;
    }
}
