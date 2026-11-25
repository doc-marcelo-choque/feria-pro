using System.ComponentModel.DataAnnotations;
using FeriaPro.Models;
namespace FeriaPro.ViewModels;
public class EvaluacionViewModel
{
    public int TribunalId { get; set; }
    [Required, Range(typeof(decimal), "0", "100")] public decimal? Innovacion { get; set; }
    [Required, Range(typeof(decimal), "0", "100")] public decimal? DesarrolloTecnico { get; set; }
    [Required, Range(typeof(decimal), "0", "100")] public decimal? Presentacion { get; set; }
    [StringLength(2000)] public string? Observaciones { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever, Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public Proyecto? Proyecto { get; set; }
}
public class TribunalViewModel
{
    [Range(1, int.MaxValue)] public int ProyectoId { get; set; }
    [Range(1, int.MaxValue)] public int ProfesorId { get; set; }
}
