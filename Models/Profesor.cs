using System.ComponentModel.DataAnnotations;
namespace FeriaPro.Models;
public class Profesor
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [MaxLength(150)] public string Nombre { get; set; } = "";
    [MaxLength(150)] public string Especialidad { get; set; } = "";
    public ICollection<Tribunal> Tribunales { get; set; } = new List<Tribunal>();
}
