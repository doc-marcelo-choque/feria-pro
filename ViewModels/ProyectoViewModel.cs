using System.ComponentModel.DataAnnotations;
namespace FeriaPro.ViewModels;
public class ProyectoViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int FeriaId { get; set; }
    [Range(1, int.MaxValue)] public int GrupoId { get; set; }
    [Required, StringLength(200)] public string Titulo { get; set; } = "";
    [Required, StringLength(5000)] public string Descripcion { get; set; } = "";
    public IFormFile? Fotografia { get; set; }
    [Required, StringLength(2048)] public string PdfUrl { get; set; } = "";
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (!Uri.TryCreate(PdfUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo))
            yield return new("El documento debe tener una URL HTTPS válida.", [nameof(PdfUrl)]);
    }
}
