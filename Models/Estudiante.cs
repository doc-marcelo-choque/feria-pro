using System.ComponentModel.DataAnnotations;
namespace FeriaPro.Models;
public class Estudiante
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    [MaxLength(40)] public string Codigo { get; set; } = "";
    [MaxLength(150)] public string Nombre { get; set; } = "";
    [MaxLength(150)] public string Carrera { get; set; } = "";
    public ICollection<GrupoEstudiante> GrupoEstudiantes { get; set; } = new List<GrupoEstudiante>();
}
