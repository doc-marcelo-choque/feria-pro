using FeriaPro.Models;
using Microsoft.EntityFrameworkCore;
namespace FeriaPro.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Estudiante> Estudiantes => Set<Estudiante>();
    public DbSet<Profesor> Profesores => Set<Profesor>();
    public DbSet<Feria> Ferias => Set<Feria>();
    public DbSet<Grupo> Grupos => Set<Grupo>();
    public DbSet<GrupoEstudiante> GrupoEstudiantes => Set<GrupoEstudiante>();
    public DbSet<Proyecto> Proyectos => Set<Proyecto>();
    public DbSet<Tribunal> Tribunales => Set<Tribunal>();
    public DbSet<Evaluacion> Evaluaciones => Set<Evaluacion>();

    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Usuario>().HasIndex(x => x.Correo).IsUnique();
        model.Entity<Estudiante>().HasIndex(x => x.Codigo).IsUnique();
        model.Entity<Estudiante>().HasOne(x => x.Usuario).WithOne(x => x.Estudiante).HasForeignKey<Estudiante>(x => x.UsuarioId);
        model.Entity<Profesor>().HasOne(x => x.Usuario).WithOne(x => x.Profesor).HasForeignKey<Profesor>(x => x.UsuarioId);
        model.Entity<Grupo>().HasOne(x => x.ResponsableEstudiante).WithMany().HasForeignKey(x => x.ResponsableEstudianteId);
        model.Entity<GrupoEstudiante>().HasIndex(x => new { x.GrupoId, x.EstudianteId }).IsUnique();
        model.Entity<Proyecto>().HasIndex(x => new { x.FeriaId, x.GrupoId }).IsUnique();
        model.Entity<Tribunal>().HasIndex(x => new { x.ProyectoId, x.ProfesorId }).IsUnique();
        model.Entity<Evaluacion>().HasOne(x => x.Tribunal).WithOne(x => x.Evaluacion).HasForeignKey<Evaluacion>(x => x.TribunalId);
        model.Entity<Evaluacion>().Property(x => x.Innovacion).HasPrecision(5, 2);
        model.Entity<Evaluacion>().Property(x => x.DesarrolloTecnico).HasPrecision(5, 2);
        model.Entity<Evaluacion>().Property(x => x.Presentacion).HasPrecision(5, 2);
        foreach (var fk in model.Model.GetEntityTypes().SelectMany(x => x.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }
}
