using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FeriaPro.Data;
using FeriaPro.Models;
using FeriaPro.Services;
using FeriaPro.ViewModels;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

var raiz = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
var conexionOriginal = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(raiz, "appsettings.json")))
    .RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString()!;
var nombrePruebas = "feriapro_pruebas_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
var cadena = new MySqlConnector.MySqlConnectionStringBuilder(conexionOriginal) { Database = nombrePruebas };
await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseMySql(cadena.ConnectionString, new MariaDbServerVersion(new Version(10,4,32))).Options);
await db.Database.MigrateAsync();
var listener = new TcpListener(IPAddress.Loopback, 0); listener.Start(); var puerto = ((IPEndPoint)listener.LocalEndpoint).Port; listener.Stop();
var url = $"http://127.0.0.1:{puerto}";
var password = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
var salida = new StringBuilder();
var inicio = new ProcessStartInfo("dotnet", $"\"{Path.Combine(raiz, "bin/Debug/net9.0/FeriaPro.dll")}\"") { WorkingDirectory = raiz, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
inicio.Environment["ASPNETCORE_URLS"] = url;
inicio.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
inicio.Environment["ConnectionStrings__DefaultConnection"] = cadena.ConnectionString;
inicio.Environment["AdministradorInicial__Correo"] = "admin@pruebas.invalid";
inicio.Environment["AdministradorInicial__Password"] = password;
inicio.Environment["Logging__LogLevel__Default"] = "Warning";
inicio.Environment["Logging__EventLog__LogLevel__Default"] = "None";
inicio.Environment["Logging__LogLevel__Microsoft.AspNetCore.DataProtection"] = "None";
inicio.Environment["Logging__LogLevel__Microsoft.EntityFrameworkCore"] = "Warning";
using var proceso = Process.Start(inicio)!;
proceso.OutputDataReceived += (_, e) => { lock(salida) { if(salida.Length < 30000) salida.AppendLine(e.Data); } };
proceso.ErrorDataReceived += (_, e) => { lock(salida) { if(salida.Length < 30000) salida.AppendLine(e.Data); } };
proceso.BeginOutputReadLine(); proceso.BeginErrorReadLine();
var aprobadas = new List<string>();
HttpClient Cliente() => new(new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() }) { BaseAddress = new Uri(url), Timeout = TimeSpan.FromSeconds(10) };
void Comprobar(bool condicion, string mensaje) { if (!condicion) throw new Exception(mensaje); aprobadas.Add(mensaje); Console.WriteLine("OK " + mensaje); }
async Task<string> Pagina(HttpClient cliente, string ruta)
{
    var respuesta = await cliente.GetAsync(ruta); var html = await respuesta.Content.ReadAsStringAsync();
    if (respuesta.StatusCode != HttpStatusCode.OK) throw new Exception($"GET {ruta}: {respuesta.StatusCode}\n{html[..Math.Min(html.Length,3000)]}");
    return html;
}
string Token(string html) => WebUtility.HtmlDecode(Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
async Task<HttpResponseMessage> Post(HttpClient cliente, string ruta, Dictionary<string,string> datos, string origen)
{
    var html = await Pagina(cliente, origen);
    datos["__RequestVerificationToken"] = Token(html);
    var campos = datos.ToList();
    // Los controles numéricos Razor envían estos campos para interpretar el punto decimal de HTML.
    foreach (Match campo in Regex.Matches(html, "name=\"__Invariant\"[^>]*value=\"([^\"]+)\""))
        campos.Add(new("__Invariant", WebUtility.HtmlDecode(campo.Groups[1].Value)));
    return await cliente.PostAsync(ruta, new FormUrlEncodedContent(campos));
}
async Task Redireccion(HttpResponseMessage respuesta, string paso)
{
    if (respuesta.StatusCode != HttpStatusCode.Redirect) throw new Exception(paso + ": " + respuesta.StatusCode + "\n" + await respuesta.Content.ReadAsStringAsync());
}
async Task<HttpClient> Login(string correo)
{
    var cliente = Cliente();
    await Redireccion(await Post(cliente, "/Account/Login", new() { ["Correo"] = correo, ["Password"] = password }, "/Account/Login"), "Login " + correo);
    return cliente;
}
Dictionary<string,string> DatosFeria(int id, string estado) => new() { ["Id"] = id.ToString(), ["Nombre"] = "Feria de verificación", ["FechaInicio"] = "2026-11-18", ["FechaFin"] = "2026-11-20", ["Estado"] = estado };
try
{
    using var anonimo = Cliente();
    var lista = false;
    for (var i = 0; i < 100; i++) { try { if ((await anonimo.GetAsync("/Account/Login")).IsSuccessStatusCode) { lista = true; break; } } catch (HttpRequestException) { } await Task.Delay(100); }
    Comprobar(lista, "La aplicación inicia contra una base MySQL aislada");
    Comprobar((await anonimo.GetAsync("/Usuarios")).StatusCode == HttpStatusCode.Redirect, "Anónimo no accede a administración");
    Comprobar((await anonimo.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode == HttpStatusCode.BadRequest, "POST sin antifalsificación rechazado");
    using var admin = await Login("admin@pruebas.invalid");
    await Pagina(admin, "/");
    var cuentas = new[] { ("est1", Roles.Estudiante), ("est2", Roles.Estudiante), ("prof1", Roles.Profesor), ("prof2", Roles.Profesor), ("prof3", Roles.Profesor), ("prof4", Roles.Profesor) };
    foreach (var (nombre, rol) in cuentas)
    {
        var datos = new Dictionary<string,string> { ["Correo"] = nombre + "@pruebas.invalid", ["Password"] = password, ["Rol"] = rol, ["Activo"] = "true", ["Nombre"] = nombre, ["Codigo"] = nombre, ["Carrera"] = "Ingeniería", ["Especialidad"] = "Tecnología" };
        await Redireccion(await Post(admin, "/Usuarios/Create", datos, "/Usuarios/Create"), "Crear " + nombre);
    }
    Comprobar(await db.Estudiantes.CountAsync() == 2 && await db.Profesores.CountAsync() == 4, "Cuentas y perfiles persisten en MySQL");
    Comprobar((await db.Usuarios.AsNoTracking().FirstAsync()).PasswordHash != password, "Las contraseñas se almacenan como hashes");
    foreach(var ruta in new[] { "/Usuarios", "/Estudiantes", "/Profesores", "/Ferias", "/Grupos", "/Proyectos", "/Tribunales", "/Evaluaciones", "/Reportes", "/Reportes/Profesores" }) await Pagina(admin, ruta);
    var estudiante1 = await db.Estudiantes.AsNoTracking().SingleAsync(x => x.Codigo == "est1");
    var estudiante2 = await db.Estudiantes.AsNoTracking().SingleAsync(x => x.Codigo == "est2");
    using var alumno1 = await Login("est1@pruebas.invalid"); using var alumno2 = await Login("est2@pruebas.invalid");
    await Pagina(alumno1, "/");
    Comprobar((await alumno1.GetAsync("/Usuarios")).StatusCode == HttpStatusCode.Redirect, "Estudiante no accede al CRUD de usuarios");
    await Redireccion(await Post(alumno1, "/Grupos/Create", new() { ["Nombre"] = "Grupo Uno" }, "/Grupos/Create"), "Crear grupo 1");
    await Redireccion(await Post(alumno2, "/Grupos/Create", new() { ["Nombre"] = "Grupo Dos" }, "/Grupos/Create"), "Crear grupo 2");
    var grupo1 = await db.Grupos.AsNoTracking().SingleAsync(x => x.Nombre == "Grupo Uno"); var grupo2 = await db.Grupos.AsNoTracking().SingleAsync(x => x.Nombre == "Grupo Dos");
    Comprobar(await db.GrupoEstudiantes.CountAsync() == 2 && grupo1.ResponsableEstudianteId == estudiante1.Id, "El creador es integrante y responsable sin datos del navegador");
    await Redireccion(await Post(alumno1, $"/Grupos/Agregar/{grupo1.Id}", new() { ["EstudianteId"] = estudiante2.Id.ToString() }, $"/Grupos/Details/{grupo1.Id}"), "Agregar integrante compatible");
    Comprobar(await db.GrupoEstudiantes.CountAsync(x => x.GrupoId == grupo1.Id) == 2, "Permite agregar un integrante compatible");
    Comprobar((await alumno2.GetAsync($"/Grupos/Edit/{grupo1.Id}")).StatusCode == HttpStatusCode.Redirect, "Un integrante que no es responsable no administra el grupo");
    await Redireccion(await Post(alumno1, $"/Grupos/Retirar/{grupo1.Id}", new() { ["EstudianteId"] = estudiante2.Id.ToString() }, $"/Grupos/Details/{grupo1.Id}"), "Retirar integrante sin bloqueo");
    await Redireccion(await Post(alumno1, $"/Grupos/Retirar/{grupo1.Id}", new() { ["EstudianteId"] = estudiante1.Id.ToString() }, $"/Grupos/Details/{grupo1.Id}"), "Intento de retirar responsable");
    Comprobar(await db.GrupoEstudiantes.CountAsync(x => x.GrupoId == grupo1.Id) == 1, "El responsable no se puede retirar ni dejar el grupo vacío");
    Comprobar((await alumno2.GetAsync($"/Grupos/Details/{grupo1.Id}")).StatusCode == HttpStatusCode.Redirect, "No se pueden consultar grupos ajenos");
    await Redireccion(await Post(admin, "/Ferias/Create", DatosFeria(0, EstadosFeria.Abierta), "/Ferias/Create"), "Crear feria");
    var feria = await db.Ferias.AsNoTracking().SingleAsync();
    var fechasInvalidas = DatosFeria(feria.Id, EstadosFeria.Abierta); fechasInvalidas["FechaFin"] = "2026-11-01";
    Comprobar((await Post(admin, $"/Ferias/Edit/{feria.Id}", fechasInvalidas, $"/Ferias/Edit/{feria.Id}")).StatusCode == HttpStatusCode.OK, "Valida que la fecha final sea posterior al inicio");
    async Task ProyectoInvalido(string pdf, byte[]? foto = null, string archivo = "foto.png")
    {
        var contenido = new MultipartFormDataContent();
        var valores = new Dictionary<string,string> { ["Titulo"] = "Inválido", ["Descripcion"] = "Intento inválido", ["GrupoId"] = grupo1.Id.ToString(), ["FeriaId"] = feria.Id.ToString(), ["PdfUrl"] = pdf, ["__RequestVerificationToken"] = Token(await Pagina(alumno1, "/Proyectos/Create")) };
        foreach(var par in valores) contenido.Add(new StringContent(par.Value), par.Key);
        if(foto != null) contenido.Add(new ByteArrayContent(foto), "Fotografia", archivo);
        var respuesta = await alumno1.PostAsync("/Proyectos/Create", contenido);
        Comprobar(respuesta.StatusCode == HttpStatusCode.OK && !await db.Proyectos.AnyAsync(), "Rechaza proyecto con documento o fotografía inválidos: " + archivo);
    }
    await ProyectoInvalido("http://example.org/inseguro.pdf");
    await ProyectoInvalido("javascript:alert(1)");
    await ProyectoInvalido("https://example.org/doc.pdf", Encoding.UTF8.GetBytes("archivo de texto"));
    await ProyectoInvalido("https://example.org/doc.pdf", Encoding.UTF8.GetBytes("<svg></svg>"), "foto.svg");
    await ProyectoInvalido("https://example.org/doc.pdf", new byte[5 * 1024 * 1024 + 1]);
    async Task CrearProyecto(HttpClient alumno, Grupo grupo, string titulo)
    {
        var formulario = new MultipartFormDataContent();
        var datos = new Dictionary<string,string> { ["Titulo"] = titulo, ["Descripcion"] = "Proyecto de integración con persistencia real", ["GrupoId"] = grupo.Id.ToString(), ["FeriaId"] = feria.Id.ToString(), ["PdfUrl"] = "https://example.org/documento.pdf", ["__RequestVerificationToken"] = Token(await Pagina(alumno, "/Proyectos/Create")) };
        foreach(var par in datos) formulario.Add(new StringContent(par.Value), par.Key);
        using var bitmap = new SKBitmap(4,4); bitmap.Erase(SKColors.Blue); using var imagen = SKImage.FromBitmap(bitmap); using var bytes = imagen.Encode(SKEncodedImageFormat.Png, 100);
        formulario.Add(new ByteArrayContent(bytes.ToArray()), "Fotografia", "foto.png");
        await Redireccion(await alumno.PostAsync("/Proyectos/Create", formulario), "Crear " + titulo);
    }
    await CrearProyecto(alumno1, grupo1, "Proyecto Uno"); await CrearProyecto(alumno2, grupo2, "Proyecto Dos");
    var proyecto1 = await db.Proyectos.AsNoTracking().SingleAsync(x => x.Titulo == "Proyecto Uno"); var proyecto2 = await db.Proyectos.AsNoTracking().SingleAsync(x => x.Titulo == "Proyecto Dos");
    Comprobar(proyecto1.FotoUrl != null && File.Exists(Path.Combine(raiz, "wwwroot", proyecto1.FotoUrl.TrimStart('/'))), "La fotografía válida se decodifica y guarda con nombre único");
    Comprobar((await alumno2.GetAsync($"/Proyectos/Edit/{proyecto1.Id}")).StatusCode == HttpStatusCode.NotFound, "No se puede editar un proyecto ajeno");
    var duplicado = new Dictionary<string,string> { ["Titulo"] = "Duplicado", ["Descripcion"] = "Intento duplicado", ["GrupoId"] = grupo1.Id.ToString(), ["FeriaId"] = feria.Id.ToString(), ["PdfUrl"] = "https://example.org/doc.pdf" };
    Comprobar((await Post(alumno1, "/Proyectos/Create", duplicado, "/Proyectos/Create")).StatusCode == HttpStatusCode.OK && await db.Proyectos.CountAsync(x => x.GrupoId == grupo1.Id) == 1, "Un grupo no registra dos proyectos en la misma feria");
    await Redireccion(await Post(admin, $"/Usuarios/Delete/{estudiante1.UsuarioId}", new(), "/Usuarios"), "Intento de eliminar con historial");
    Comprobar(await db.Usuarios.AnyAsync(x => x.Id == estudiante1.UsuarioId), "No elimina cuentas con relaciones históricas");
    var adminId = await db.Usuarios.Where(x => x.Rol == Roles.Administrador).Select(x => x.Id).SingleAsync();
    await Redireccion(await Post(admin, $"/Usuarios/Desactivar/{adminId}", new(), "/Usuarios"), "Intento de desactivar administrador propio");
    Comprobar(await db.Usuarios.AnyAsync(x => x.Id == adminId && x.Activo), "Protege la propia cuenta y el último administrador");
    Comprobar((await anonimo.GetAsync($"/Ranking/Details/{feria.Id}")).StatusCode == HttpStatusCode.NotFound, "No hay ranking público durante inscripciones");
    await Redireccion(await Post(alumno1, $"/Grupos/Agregar/{grupo1.Id}", new() { ["EstudianteId"] = estudiante2.Id.ToString() }, $"/Grupos/Details/{grupo1.Id}"), "Intento de participación incompatible");
    Comprobar(await db.GrupoEstudiantes.CountAsync(x => x.GrupoId == grupo1.Id) == 1, "No permite participar con dos grupos en la misma feria");
    var incompleta = await Post(admin, $"/Ferias/Edit/{feria.Id}", DatosFeria(feria.Id, EstadosFeria.EnEvaluacion), $"/Ferias/Edit/{feria.Id}");
    Comprobar(incompleta.StatusCode == HttpStatusCode.OK && (await db.Ferias.AsNoTracking().SingleAsync()).Estado == EstadosFeria.Abierta, "No inicia evaluación sin tres profesores");
    var profesores = await db.Profesores.AsNoTracking().OrderBy(x => x.Id).ToListAsync();
    foreach(var proyecto in new[] { proyecto1, proyecto2 }) foreach(var profesor in profesores.Take(3))
        await Redireccion(await Post(admin, "/Tribunales/Asignar", new() { ["ProyectoId"] = proyecto.Id.ToString(), ["ProfesorId"] = profesor.Id.ToString() }, "/Tribunales"), "Asignar tribunal");
    await Redireccion(await Post(admin, "/Tribunales/Asignar", new() { ["ProyectoId"] = proyecto1.Id.ToString(), ["ProfesorId"] = profesores[0].Id.ToString() }, "/Tribunales"), "Asignación duplicada");
    Comprobar(await db.Tribunales.CountAsync(x => x.ProyectoId == proyecto1.Id) == 3, "No permite tribunales duplicados ni más de tres");
    await Redireccion(await Post(admin, $"/Ferias/Edit/{feria.Id}", DatosFeria(feria.Id, EstadosFeria.EnEvaluacion), $"/Ferias/Edit/{feria.Id}"), "Iniciar evaluación");
    Comprobar((await alumno1.GetAsync($"/Proyectos/Edit/{proyecto1.Id}")).StatusCode == HttpStatusCode.Redirect, "Proyecto bloqueado durante evaluación");
    Comprobar((await alumno1.GetAsync($"/Grupos/Edit/{grupo1.Id}")).StatusCode == HttpStatusCode.Redirect, "No cambia el grupo durante la evaluación");
    var pendiente = await Post(admin, $"/Ferias/Edit/{feria.Id}", DatosFeria(feria.Id, EstadosFeria.Finalizada), $"/Ferias/Edit/{feria.Id}");
    Comprobar(pendiente.StatusCode == HttpStatusCode.OK && (await db.Ferias.AsNoTracking().SingleAsync()).Estado == EstadosFeria.EnEvaluacion, "No finaliza una feria con evaluaciones pendientes");
    using var noAsignado = await Login("prof4@pruebas.invalid");
    var primerTribunal = await db.Tribunales.AsNoTracking().FirstAsync();
    Comprobar((await noAsignado.GetAsync($"/Evaluaciones/Edit/{primerTribunal.Id}")).StatusCode == HttpStatusCode.NotFound, "Profesor no asignado no accede a la evaluación");
    Comprobar((await Post(noAsignado, $"/Evaluaciones/Edit/{primerTribunal.Id}", new() { ["TribunalId"] = primerTribunal.Id.ToString(), ["Innovacion"] = "100", ["DesarrolloTecnico"] = "100", ["Presentacion"] = "100" }, "/Evaluaciones")).StatusCode == HttpStatusCode.NotFound, "POST falsificado de un profesor no asignado es rechazado");
    for(var i = 0; i < 3; i++)
    {
        using var profesor = await Login($"prof{i+1}@pruebas.invalid"); await Pagina(profesor, "/");
        var tribunales = await db.Tribunales.AsNoTracking().Where(x => x.ProfesorId == profesores[i].Id).ToListAsync();
        foreach(var tribunal in tribunales)
        {
            var datos = new Dictionary<string,string> { ["TribunalId"] = tribunal.Id.ToString(), ["Innovacion"] = "90", ["DesarrolloTecnico"] = "80", ["Presentacion"] = "70", ["Observaciones"] = "Evaluación de integración" };
            var ruta = $"/Evaluaciones/Edit/{tribunal.Id}";
            if(i == 0)
            {
                datos["Innovacion"] = "101";
                Comprobar((await Post(profesor, ruta, datos, ruta)).StatusCode == HttpStatusCode.OK, "Rechaza calificaciones superiores a 100"); datos["Innovacion"] = "90";
                datos["Presentacion"] = "-1";
                Comprobar((await Post(profesor, ruta, datos, ruta)).StatusCode == HttpStatusCode.OK, "Rechaza calificaciones negativas"); datos["Presentacion"] = "70";
                datos["Presentacion"] = "70.123";
                Comprobar((await Post(profesor, ruta, datos, ruta)).StatusCode == HttpStatusCode.OK, "Rechaza precisión superior a dos decimales"); datos["Presentacion"] = "70";
            }
            await Redireccion(await Post(profesor, ruta, datos, ruta), "Guardar evaluación");
            if(i == 0)
            {
                datos["Innovacion"] = "90.25";
                await Redireccion(await Post(profesor, ruta, datos, ruta), "Guardar criterio decimal");
                Comprobar((await db.Evaluaciones.AsNoTracking().SingleAsync(x => x.TribunalId == tribunal.Id)).Nota == 82.1m, "Conserva criterios y ponderaciones decimales");
                datos["Innovacion"] = "90";
            }
            await Redireccion(await Post(profesor, ruta, datos, ruta), "Modificar propia evaluación");
        }
    }
    Comprobar(await db.Evaluaciones.CountAsync() == 6, "La edición no genera evaluaciones duplicadas");
    Comprobar((await db.Evaluaciones.AsNoTracking().ToListAsync()).All(x => x.Nota == 82m), "Nota ponderada correcta: 90×40% + 80×40% + 70×20% = 82");
    await Redireccion(await Post(admin, $"/Tribunales/Quitar/{primerTribunal.Id}", new(), "/Tribunales"), "Intento de quitar evaluación existente");
    Comprobar(await db.Tribunales.CountAsync() == 6, "Las asignaciones evaluadas no se eliminan");
    await Redireccion(await Post(admin, $"/Ferias/Edit/{feria.Id}", DatosFeria(feria.Id, EstadosFeria.Finalizada), $"/Ferias/Edit/{feria.Id}"), "Finalizar feria");
    var servicio = new RankingService(db); var ranking = (await servicio.CalcularAsync(feria.Id))!;
    Comprobar(ranking.EmpatesPendientes && ranking.Resultados.All(x => x.NotaFinal == 82), "Ranking calcula promedio y reconoce empate total");
    Comprobar((await anonimo.GetAsync($"/Ranking/Details/{feria.Id}")).StatusCode == HttpStatusCode.NotFound, "Feria finalizada sin publicar continúa privada");
    await Redireccion(await Post(admin, $"/Ranking/Publicar/{feria.Id}", new(), $"/Ranking/Details/{feria.Id}"), "Publicar sin resolver empate");
    Comprobar(!(await db.Ferias.AsNoTracking().SingleAsync()).ResultadosPublicados, "No publica un empate sin resolución");
    await Redireccion(await Post(admin, $"/Ranking/Publicar/{feria.Id}", new() { ["OrdenDesempate"] = $"{proyecto2.Id},{proyecto1.Id}", ["ResolucionEmpate"] = "Acta de comité: se resuelve el empate según la demostración complementaria de ambos proyectos." }, $"/Ranking/Details/{feria.Id}"), "Resolver y publicar");
    ranking = (await servicio.CalcularAsync(feria.Id))!;
    Comprobar(!ranking.EmpatesPendientes && ranking.Resultados[0].ProyectoId == proyecto2.Id, "La resolución administra únicamente empates y permite publicar");
    await Pagina(anonimo, $"/Ranking/Details/{feria.Id}");
    Comprobar((await alumno1.GetAsync($"/Proyectos/Edit/{proyecto1.Id}")).StatusCode == HttpStatusCode.Redirect, "Los proyectos finalizados permanecen bloqueados");
    using var profesorFinal = await Login("prof1@pruebas.invalid");
    Comprobar((await profesorFinal.GetAsync($"/Evaluaciones/Edit/{primerTribunal.Id}")).StatusCode == HttpStatusCode.Redirect, "No modifica evaluaciones finalizadas");
    var cambiarNota = await Post(profesorFinal, $"/Evaluaciones/Edit/{primerTribunal.Id}", new() { ["TribunalId"] = primerTribunal.Id.ToString(), ["Innovacion"] = "0", ["DesarrolloTecnico"] = "0", ["Presentacion"] = "0" }, "/Evaluaciones");
    Comprobar(cambiarNota.StatusCode == HttpStatusCode.Redirect && (await db.Evaluaciones.AsNoTracking().ToListAsync()).All(x => x.Nota == 82), "POST directo no cambia notas tras finalizar");
    Comprobar((await Post(admin, $"/Ferias/Edit/{feria.Id}", DatosFeria(feria.Id, EstadosFeria.Abierta), $"/Ferias/Edit/{feria.Id}")).StatusCode == HttpStatusCode.OK && (await db.Ferias.AsNoTracking().SingleAsync()).Estado == EstadosFeria.Finalizada, "No se reabre una feria finalizada");
    foreach(var reporte in new[] { "Proyectos", "Evaluaciones", "Profesores", "Ranking" })
    {
        await Pagina(admin, $"/Reportes/{reporte}?feriaId={feria.Id}");
        var csv = await admin.GetAsync($"/Reportes/{reporte}?feriaId={feria.Id}&csv=true");
        Comprobar(csv.IsSuccessStatusCode && csv.Content.Headers.ContentType?.MediaType == "text/csv", "Reporte HTML y CSV: " + reporte);
    }
    await Pagina(admin, "/");
    var rolGuardado = await db.Usuarios.AsNoTracking().SingleAsync(x => x.Id == estudiante1.UsuarioId);
    await Redireccion(await Post(admin, $"/Usuarios/Edit/{rolGuardado.Id}", new() { ["Id"] = rolGuardado.Id.ToString(), ["Correo"] = rolGuardado.Correo, ["Rol"] = Roles.Estudiante, ["Nombre"] = "Estudiante actualizado", ["Codigo"] = estudiante1.Codigo, ["Carrera"] = estudiante1.Carrera, ["Activo"] = "true" }, $"/Usuarios/Edit/{rolGuardado.Id}"), "Editar sin contraseña");
    Comprobar((await db.Usuarios.AsNoTracking().SingleAsync(x => x.Id == rolGuardado.Id)).PasswordHash == rolGuardado.PasswordHash, "Editar sin contraseña conserva el hash");
    await Redireccion(await Post(admin, $"/Usuarios/Desactivar/{rolGuardado.Id}", new(), "/Usuarios"), "Desactivar cuenta");
    Comprobar((await alumno1.GetAsync("/")).StatusCode == HttpStatusCode.Redirect, "Desactivar cuenta revoca una sesión existente");
    var noAsignadoId = profesores[3].UsuarioId;
    await Redireccion(await Post(admin, $"/Profesores/Delete/{noAsignadoId}", new(), "/Profesores"), "Eliminar profesor sin historial");
    Comprobar(!await db.Usuarios.AnyAsync(x => x.Id == noAsignadoId) && !await db.Profesores.AnyAsync(x => x.UsuarioId == noAsignadoId), "Elimina cuenta y perfil sin referencias mediante EF");
    var datosOrden = new[] { new ResultadoRanking { ProyectoId = 1, NotaFinal = 80, DesarrolloTecnico = 90 }, new ResultadoRanking { ProyectoId = 2, NotaFinal = 80, DesarrolloTecnico = 95 } };
    Comprobar(RankingService.Ordenar(datosOrden, [1,2])[0].ProyectoId == 2, "El desarrollo técnico prima sobre una resolución manual");
    Console.WriteLine($"VERIFICACIÓN COMPLETA: {aprobadas.Count} comprobaciones. Base aislada: {nombrePruebas}");
}
catch (Exception error) { Console.Error.WriteLine(salida.ToString()); Console.Error.WriteLine(error); Environment.ExitCode = 1; }
finally { if (!proceso.HasExited) { proceso.Kill(true); await proceso.WaitForExitAsync(); } }
