namespace FeriaPro.Models;
public class GrupoEstudiante
{
    public int Id { get; set; }
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;
    public int EstudianteId { get; set; }
    public Estudiante Estudiante { get; set; } = null!;
}
