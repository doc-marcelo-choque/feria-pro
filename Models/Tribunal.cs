namespace FeriaPro.Models;
public class Tribunal
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public Proyecto Proyecto { get; set; } = null!;
    public int ProfesorId { get; set; }
    public Profesor Profesor { get; set; } = null!;
    public Evaluacion? Evaluacion { get; set; }
}
