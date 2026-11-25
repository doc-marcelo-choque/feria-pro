using System.ComponentModel.DataAnnotations;
namespace FeriaPro.Models;
public class Grupo
{
    public int Id { get; set; }
    [MaxLength(100)] public string Nombre { get; set; } = "";
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
    public int ResponsableEstudianteId { get; set; }
    public Estudiante ResponsableEstudiante { get; set; } = null!;
    public ICollection<GrupoEstudiante> GrupoEstudiantes { get; set; } = new List<GrupoEstudiante>();
    public ICollection<Proyecto> Proyectos { get; set; } = new List<Proyecto>();
}
