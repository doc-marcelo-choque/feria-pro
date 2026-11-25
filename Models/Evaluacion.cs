using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace FeriaPro.Models;
public class Evaluacion
{
    public int Id { get; set; }
    public int TribunalId { get; set; }
    public Tribunal Tribunal { get; set; } = null!;
    public decimal Innovacion { get; set; }
    public decimal DesarrolloTecnico { get; set; }
    public decimal Presentacion { get; set; }
    [MaxLength(2000)] public string? Observaciones { get; set; }
    public DateTime FechaEvaluacion { get; set; } = DateTime.UtcNow;
    [NotMapped] public decimal Nota => Innovacion * .40m + DesarrolloTecnico * .40m + Presentacion * .20m;
}
