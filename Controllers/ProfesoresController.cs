using FeriaPro.Data;
using FeriaPro.Models;
namespace FeriaPro.Controllers;
public class ProfesoresController(AppDbContext db) : UsuariosController(db)
{
    protected override string? TipoPerfil => Roles.Profesor;
}
