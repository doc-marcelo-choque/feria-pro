namespace FeriaPro.ViewModels;
public class DashboardViewModel
{
    public Dictionary<string, int> Estadisticas { get; set; } = [];
    public List<string> Ferias { get; set; } = [];
    public List<int> ProyectosPorFeria { get; set; } = [];
    public List<string> Estados { get; set; } = [];
    public List<int> ProyectosPorEstado { get; set; } = [];
    public int Evaluados { get; set; }
    public int Pendientes { get; set; }
    public List<Models.Feria> FeriasDisponibles { get; set; } = [];
}
