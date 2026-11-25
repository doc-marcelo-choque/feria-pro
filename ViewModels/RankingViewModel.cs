using System.ComponentModel.DataAnnotations;
using FeriaPro.Models;
namespace FeriaPro.ViewModels;
public class RankingViewModel
{
    public Feria Feria { get; set; } = null!;
    public int Participantes { get; set; }
    public List<ResultadoRanking> Resultados { get; set; } = [];
    public bool EmpatesPendientes => Resultados.Any(x => x.Empatado);
}
public class ResultadoRanking
{
    public int Posicion { get; set; }
    public int ProyectoId { get; set; }
    public string Titulo { get; set; } = "";
    public string Grupo { get; set; } = "";
    public string? FotoUrl { get; set; }
    public int Evaluaciones { get; set; }
    public decimal NotaFinal { get; set; }
    public decimal DesarrolloTecnico { get; set; }
    public decimal Innovacion { get; set; }
    public decimal Presentacion { get; set; }
    public bool Empatado { get; set; }
}
public class PublicarRankingViewModel
{
    [StringLength(4000)] public string? OrdenDesempate { get; set; }
    [StringLength(2000)] public string? ResolucionEmpate { get; set; }
}
