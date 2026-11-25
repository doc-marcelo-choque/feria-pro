using System.ComponentModel.DataAnnotations;
namespace FeriaPro.ViewModels;
public class GrupoViewModel
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Nombre { get; set; } = "";
}
public class IntegranteViewModel
{
    [Range(1, int.MaxValue)] public int EstudianteId { get; set; }
}
