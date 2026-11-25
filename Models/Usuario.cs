using System.ComponentModel.DataAnnotations;
namespace FeriaPro.Models;

public class Usuario
{
    public int Id { get; set; }
    [MaxLength(254)] public string Correo { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    [MaxLength(20)] public string Rol { get; set; } = Roles.Estudiante;
    public bool Activo { get; set; } = true;
    public Estudiante? Estudiante { get; set; }
    public Profesor? Profesor { get; set; }
}

public static class Roles
{
    public const string Administrador = "Administrador", Profesor = "Profesor", Estudiante = "Estudiante";
    public static readonly string[] Todos = [Administrador, Profesor, Estudiante];
}
