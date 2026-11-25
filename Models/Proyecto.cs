using System.ComponentModel.DataAnnotations;
namespace FeriaPro.Models;
public class Proyecto
{
    public int Id { get; set; }
    public int FeriaId { get; set; }
    public Feria Feria { get; set; } = null!;
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;
    [MaxLength(200)] public string Titulo { get; set; } = "";
    [MaxLength(5000)] public string Descripcion { get; set; } = "";
    [MaxLength(300)] public string? FotoUrl { get; set; }
    [MaxLength(2048)] public string PdfUrl { get; set; } = "";
    [MaxLength(20)] public string Estado { get; set; } = "Registrado";
    public ICollection<Tribunal> Tribunales { get; set; } = new List<Tribunal>();
}
