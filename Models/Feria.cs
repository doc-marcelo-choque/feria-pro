using System.ComponentModel.DataAnnotations;
namespace FeriaPro.Models;
public class Feria
{
    public int Id { get; set; }
    [MaxLength(150)] public string Nombre { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    [MaxLength(20)] public string Estado { get; set; } = EstadosFeria.Abierta;
    public bool ResultadosPublicados { get; set; }
    public string? OrdenDesempate { get; set; }
    public string? ResolucionEmpate { get; set; }
    public int? PublicadoPorUsuarioId { get; set; }
    public Usuario? PublicadoPorUsuario { get; set; }
    public DateTime? FechaPublicacion { get; set; }
    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
public static class EstadosFeria
{
    public const string Abierta = "Abierta", EnEvaluacion = "EnEvaluacion", Finalizada = "Finalizada";
    public static readonly string[] Todos = [Abierta, EnEvaluacion, Finalizada];
}
