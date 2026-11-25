using SkiaSharp;
namespace FeriaPro.Services;
public class ArchivoService(IWebHostEnvironment entorno)
{
    public async Task<string> GuardarAsync(IFormFile archivo)
    {
        if (archivo.Length is <= 0 or > 5 * 1024 * 1024) throw new ArgumentException("La fotografía debe tener un tamaño de hasta 5 MB.");
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp")) throw new ArgumentException("Formatos permitidos: JPG, PNG y WebP.");
        using var memoria = new MemoryStream(); await archivo.CopyToAsync(memoria);
        using var datos = SKData.CreateCopy(memoria.ToArray());
        using var codec = SKCodec.Create(datos);
        if (codec == null || codec.Info.Width <= 0 || codec.Info.Height <= 0 || (long)codec.Info.Width * codec.Info.Height > 20_000_000)
            throw new ArgumentException("Imagen inválida o demasiado grande (máximo 20 megapíxeles).");
        var formato = extension == ".png" ? SKEncodedImageFormat.Png : extension == ".webp" ? SKEncodedImageFormat.Webp : SKEncodedImageFormat.Jpeg;
        if (codec.EncodedFormat != formato) throw new ArgumentException("El contenido de la imagen no coincide con su extensión.");
        using var bitmap = new SKBitmap(codec.Info.Width, codec.Info.Height);
        if (codec.GetPixels(bitmap.Info, bitmap.GetPixels()) != SKCodecResult.Success) throw new ArgumentException("El archivo no contiene una imagen completa y válida.");
        // Volver a codificar evita publicar metadatos y contenido adicional del archivo original.
        using var imagen = SKImage.FromBitmap(bitmap);
        using var normalizada = imagen.Encode(SKEncodedImageFormat.Png, 100);
        var carpeta = Path.Combine(entorno.WebRootPath, "uploads", "proyectos"); Directory.CreateDirectory(carpeta);
        var nombre = $"{Guid.NewGuid():N}.png";
        await File.WriteAllBytesAsync(Path.Combine(carpeta, nombre), normalizada.ToArray());
        return $"/uploads/proyectos/{nombre}";
    }
    public void Eliminar(string? ruta)
    {
        if (ruta == null || !ruta.StartsWith("/uploads/proyectos/", StringComparison.Ordinal)) return;
        var nombre = Path.GetFileName(ruta);
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(nombre), "N", out _)) return;
        var path = Path.Combine(entorno.WebRootPath, "uploads", "proyectos", nombre);
        if (File.Exists(path)) File.Delete(path);
    }
}
