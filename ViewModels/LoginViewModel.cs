using System.ComponentModel.DataAnnotations;
namespace FeriaPro.ViewModels;
public class LoginViewModel
{
    [Required, EmailAddress] public string Correo { get; set; } = "";
    [Required, DataType(DataType.Password)] public string Password { get; set; } = "";
    public bool Recordarme { get; set; }
    public string? ReturnUrl { get; set; }
}
