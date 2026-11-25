using System.ComponentModel.DataAnnotations;
using FeriaPro.Models;
namespace FeriaPro.ViewModels;
public class UsuarioViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required, EmailAddress, StringLength(254)] public string Correo { get; set; } = "";
    [DataType(DataType.Password), StringLength(100, MinimumLength = 10)] public string? Password { get; set; }
    [Required] public string Rol { get; set; } = Roles.Estudiante;
    public bool Activo { get; set; } = true;
    [StringLength(40)] public string? Codigo { get; set; }
    [StringLength(150)] public string? Nombre { get; set; }
    [StringLength(150)] public string? Carrera { get; set; }
    [StringLength(150)] public string? Especialidad { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Id == 0 && string.IsNullOrWhiteSpace(Password)) yield return new("La contraseña es obligatoria al crear.", [nameof(Password)]);
        if (!Roles.Todos.Contains(Rol)) yield return new("Rol no permitido.", [nameof(Rol)]);
        if (Rol != Roles.Administrador && string.IsNullOrWhiteSpace(Nombre)) yield return new("El nombre es obligatorio.", [nameof(Nombre)]);
        if (Rol == Roles.Estudiante)
        {
            if (string.IsNullOrWhiteSpace(Codigo)) yield return new("El código es obligatorio.", [nameof(Codigo)]);
            if (string.IsNullOrWhiteSpace(Carrera)) yield return new("La carrera es obligatoria.", [nameof(Carrera)]);
        }
        if (Rol == Roles.Profesor && string.IsNullOrWhiteSpace(Especialidad)) yield return new("La especialidad es obligatoria.", [nameof(Especialidad)]);
    }
}
