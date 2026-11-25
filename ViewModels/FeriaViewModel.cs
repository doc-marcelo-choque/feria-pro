using System.ComponentModel.DataAnnotations;
using FeriaPro.Models;
namespace FeriaPro.ViewModels;
public class FeriaViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Nombre { get; set; } = "";
    [Required, DataType(DataType.Date)] public DateTime FechaInicio { get; set; } = DateTime.Today;
    [Required, DataType(DataType.Date)] public DateTime FechaFin { get; set; } = DateTime.Today;
    [Required] public string Estado { get; set; } = EstadosFeria.Abierta;
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (FechaFin < FechaInicio) yield return new("La fecha final no puede ser anterior al inicio.", [nameof(FechaFin)]);
        if (!EstadosFeria.Todos.Contains(Estado)) yield return new("Estado no permitido.", [nameof(Estado)]);
    }
}
