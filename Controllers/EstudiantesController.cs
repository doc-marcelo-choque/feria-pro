using FeriaPro.Data;
using FeriaPro.Models;
namespace FeriaPro.Controllers;
public class EstudiantesController(AppDbContext db) : UsuariosController(db)
{
    protected override string? TipoPerfil => Roles.Estudiante;
}
