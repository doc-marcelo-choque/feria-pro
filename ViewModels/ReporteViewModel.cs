using FeriaPro.Models;
namespace FeriaPro.ViewModels;
public class ReporteViewModel
{
    public string Titulo { get; set; } = "";
    public string[] Columnas { get; set; } = [];
    public List<string[]> Filas { get; set; } = [];
    public List<Feria> Ferias { get; set; } = [];
    public int? FeriaId { get; set; }
}
