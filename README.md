# FeriaPro

Aplicación ASP.NET Core MVC (.NET 9), EF Core 9, Pomelo y MySQL/MariaDB de XAMPP. La conexión de `appsettings.json` se conserva.

## Arranque

1. Iniciar MySQL desde XAMPP. El servidor verificado es MariaDB 10.4.32, puerto 3306.
2. Ejecutar `dotnet restore` y `dotnet build`.
3. En una instalación nueva, aplicar `dotnet ef database update`. La migración inicial crea las nueve tablas y sus índices; ya se aplicó a `feriapro` durante esta implementación. No usar `EnsureCreated`, eliminar migraciones ni reiniciar la base.
4. Configurar el primer administrador con variables de entorno y arrancar:

```powershell
$env:AdministradorInicial__Correo = Read-Host 'Correo del administrador'
$claveInicial = Read-Host 'Contraseña (mínimo 10 caracteres)' -AsSecureString
$env:AdministradorInicial__Password = [System.Net.NetworkCredential]::new('', $claveInicial).Password
dotnet run --launch-profile https
```

Abrir `https://localhost:7034`. El administrador se crea solo si todavía no existe ninguno. No hay una contraseña predeterminada. Después de crearlo, se pueden retirar esas variables del entorno. Nunca guardar credenciales reales en el repositorio.

La versión del servidor se configura en `Program.cs` según la instancia XAMPP inspeccionada. Si se usa otra versión de MySQL/MariaDB, adaptar únicamente ese dato. La configuración sigue las [opciones oficiales de Pomelo](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/wiki/Configuration-Options).

## Funcionalidades y archivos

- `Models/` y `Data/AppDbContext.cs`: nueve entidades, relaciones restrictivas, calificaciones decimales e índices únicos. `Grupo.ResponsableEstudianteId` identifica al creador; `Feria` conserva publicación, autor, fecha y resolución de empates.
- `Migrations/`: migración inicial y snapshot. `InicialFeriaPro.sql` permite revisar el SQL generado; aplicar los cambios mediante EF.
- `Program.cs`, `Controllers/AccountController.cs`, `Services/AdministradorInicial.cs`, `Services/SesionCookieEvents.cs`: cookies, claims, roles, hashes, inicio y cierre de sesión, límite de intentos y revocación de cuentas inactivas o con contraseña/rol modificados.
- `Controllers/UsuariosController.cs`, `EstudiantesController.cs`, `ProfesoresController.cs` y `Views/Usuarios/`: cuentas y perfiles en una sola operación. Los dos controladores de perfiles reutilizan las acciones y vistas, restringiendo el rol correspondiente. Búsqueda, detalle, creación, edición, desactivación y eliminación solo sin historial. No se muestra el hash.
- `Controllers/FeriasController.cs`, `ViewModels/FeriaViewModel.cs`, `Views/Ferias/`: fechas, estados, consulta y eliminación de ferias abiertas sin proyectos.
- `Controllers/GruposController.cs`, `ViewModels/GrupoViewModel.cs`, `Views/Grupos/`: creación con el estudiante autenticado como responsable, integrantes y grupos propios; protección contra participaciones incompatibles.
- `Controllers/ProyectosController.cs`, `ViewModels/ProyectoViewModel.cs`, `Services/ArchivoService.cs`, `Views/Proyectos/`: tarjetas, filtros, CRUD, pertenencia, documentos HTTPS e imágenes hasta 5 MB. SkiaSharp valida extensión, formato, dimensiones y decodificación completa; las imágenes se normalizan a PNG con nombres únicos en `wwwroot/uploads/proyectos/`. El paquete incluye componentes nativos de Windows; otro sistema operativo puede requerir sus componentes nativos correspondientes.
- `Controllers/TribunalesController.cs`, `EvaluacionesController.cs`, `ViewModels/EvaluacionViewModel.cs` y vistas: tres evaluadores, asignaciones únicas, criterios de 0 a 100 con dos decimales y ponderaciones 40/40/20 calculadas en el servidor. JavaScript muestra una vista previa.
- `Services/RankingService.cs`, `Controllers/RankingController.cs`, `ViewModels/RankingViewModel.cs`, `Views/Ranking/`: promedio, criterios de desempate, resolución administrativa documentada y resultados públicos después de la publicación.
- `Controllers/ReportesController.cs`, `ViewModels/ReporteViewModel.cs`, `Views/Reportes/`: cuatro reportes con filtros, impresión HTML y CSV UTF-8 protegido contra fórmulas.
- `Controllers/HomeController.cs`, `ViewModels/DashboardViewModel.cs`, `Views/Home/Index.cshtml`, `wwwroot/js/dashboard.js`, `wwwroot/lib/chartjs/`: paneles por rol y tres gráficos con datos reales. Las evaluaciones pendientes incluyen asignaciones todavía faltantes.
- `Services/OperacionSeguraFilter.cs`: transacciones serializables para POST y mensajes de conflicto sin exponer errores internos. Las modificaciones concurrentes pueden requerir actualizar y volver a intentar.
- `Views/Shared/_Layout.cshtml`, páginas de errores, `wwwroot/css/site.css`, `wwwroot/js/site.js`: navegación por rol, Bootstrap 5, mensajes, confirmaciones y estilos de impresión.
- `FeriaPro.csproj`: dependencias necesarias de persistencia y validación de imágenes; exclusión del código de pruebas del proyecto web. `.gitignore` excluye compilados, archivos subidos y archivos de entorno.

## Prueba manual del flujo

1. Administrador: iniciar sesión; registrar estudiantes y al menos tres profesores. Comprobar búsquedas, edición con contraseña vacía, desactivación y rechazo de correos/códigos duplicados.
2. Crear una feria abierta con fechas válidas.
3. Estudiante: crear grupo; comprobar inclusión automática del creador, agregar/retirar integrantes y registrar un proyecto con fotografía y enlace PDF HTTPS. Un estudiante no puede participar en dos proyectos de la misma feria.
4. Administrador: asignar tres profesores distintos por proyecto. Cambiar la feria a `EnEvaluacion`.
5. Profesor: consultar únicamente sus asignaciones y evaluar. Con innovación 90, desarrollo técnico 80 y presentación 70, la nota debe ser 82. Se permite editar la evaluación propia hasta finalizar.
6. Verificar que una feria con evaluaciones pendientes no se pueda finalizar y que una asignación evaluada no se pueda retirar.
7. Finalizar con todas las evaluaciones completas. Si hay empate total, introducir los IDs de todos los proyectos separados por comas y el fundamento de la resolución. El orden manual solo decide entre proyectos que siguen empatados después de los criterios técnicos.
8. Publicar y abrir Resultados sin iniciar sesión. Comprobar podio, ranking y bloqueo de modificaciones posteriores.
9. Consultar los cuatro reportes, filtrar por feria, imprimir y descargar CSV. Revisar los paneles de cada rol.

Las ferias finalizadas no se reabren. Los proyectos, integrantes y evaluaciones relacionados quedan bloqueados para conservar los resultados. Las cuentas con historial se desactivan; no se eliminan sus referencias.

## Verificación automatizada

```powershell
dotnet build
dotnet ef migrations has-pending-model-changes --no-build
dotnet run --project tests/FeriaPro.Verificacion/FeriaPro.Verificacion.csproj
```

El verificador usa HTTP real, cookies, antifalsificación y MySQL. Crea una base independiente `feriapro_pruebas_<fecha UTC>`, aplica migraciones y genera credenciales aleatorias solo para ese proceso. No modifica los registros de `feriapro`, no elimina bases y no publica datos de prueba en ella. Los registros de las bases de pruebas quedan disponibles para inspección; las fotografías usan nombres únicos dentro de la carpeta de uploads.

El entorno `Testing` usa claves temporales de protección de cookies para que la prueba no dependa de claves DPAPI de otro proceso de Windows. Usar `Development` en desarrollo habitual y `Production` con HTTPS en despliegues.

## Alcance de la comprobación

Resultado de la ejecución final: **58 comprobaciones aprobadas** contra la base aislada `feriapro_pruebas_20261118051521`. Se comprobaron persistencia, roles, propiedad, restricciones, imágenes, estados, notas decimales, empates, publicación, reportes y revocación de sesiones. La compilación también valida las vistas Razor. EF no detectó cambios pendientes respecto de la migración inicial. La base principal `feriapro` conserva sus tablas sin registros de prueba.

Pendiente: inspección visual en navegador (el control de la aplicación agotó el tiempo de autorización), adaptación de tablas y menú en móviles, vista previa de impresión y pruebas de carga concurrente. Antes del uso real se debe configurar el administrador con las credenciales elegidas por el responsable.
