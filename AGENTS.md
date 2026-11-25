# AGENTS.md — FeriaPro

## 1. Descripción general

El proyecto **FeriaPro** es un sistema web de gestión de ferias universitarias de proyectos desarrollado con ASP.NET Core MVC, .NET 9, Entity Framework Core, Razor Views, Bootstrap y MySQL de XAMPP.

El sistema permitirá administrar usuarios, estudiantes, profesores, ferias académicas, grupos de trabajo, proyectos, tribunales evaluadores, calificaciones y rankings.

Debe desarrollarse como una aplicación monolítica MVC, manteniendo una arquitectura sencilla, organizada y fácil de mantener.

## 2. Estado actual del proyecto

El proyecto ASP.NET Core MVC ya fue creado utilizando .NET 9.

La conexión con MySQL de XAMPP ya está configurada.

Antes de comenzar cualquier modificación:

1. Inspeccionar la estructura de archivos existente.
2. Revisar `FeriaPro.csproj`, `Program.cs` y `appsettings.json`.
3. Identificar los paquetes NuGet instalados.
4. Revisar los modelos, `AppDbContext` y las migraciones existentes.
5. Reutilizar las clases, relaciones y configuraciones que ya estén implementadas.
6. No recrear el proyecto.
7. No reemplazar ni eliminar la conexión existente con MySQL.
8. No eliminar migraciones ni tablas existentes.
9. No sobrescribir código funcional sin una justificación técnica.
10. Ejecutar `dotnet build` antes de realizar cambios importantes.

Si los nueve modelos ya existen, utilizarlos y ampliarlos solamente cuando sea necesario.

## 3. Tecnologías obligatorias

- Lenguaje: C#.
- Framework: .NET 9.
- Arquitectura: ASP.NET Core MVC.
- ORM: Entity Framework Core 9.
- Proveedor de base de datos: Pomelo.EntityFrameworkCore.MySql 9.
- Base de datos: MySQL o MariaDB mediante XAMPP.
- Vistas: Razor Views.
- Interfaz: Bootstrap 5.
- JavaScript: JavaScript nativo.
- Gráficos: Chart.js.
- Autenticación: ASP.NET Core Cookie Authentication.
- Contraseñas: `PasswordHasher<Usuario>`.
- Consultas: LINQ y Entity Framework Core.
- Migraciones: EF Core Migrations.

No incorporar React, Angular, Blazor, Vue, microservicios, patrones CQRS, MediatR ni una API REST independiente.

No agregar repositorios genéricos ni capas adicionales que solamente dupliquen las funciones de Entity Framework Core.

## 4. Arquitectura

Utilizar la arquitectura MVC existente.

Mantener la siguiente estructura como referencia:

```text
FeriaPro/
├── Controllers/
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── UsuariosController.cs
│   ├── EstudiantesController.cs
│   ├── ProfesoresController.cs
│   ├── FeriasController.cs
│   ├── GruposController.cs
│   ├── ProyectosController.cs
│   ├── TribunalesController.cs
│   ├── EvaluacionesController.cs
│   ├── RankingController.cs
│   └── ReportesController.cs
│
├── Models/
│   ├── Usuario.cs
│   ├── Estudiante.cs
│   ├── Profesor.cs
│   ├── Feria.cs
│   ├── Grupo.cs
│   ├── GrupoEstudiante.cs
│   ├── Proyecto.cs
│   ├── Tribunal.cs
│   └── Evaluacion.cs
│
├── Data/
│   └── AppDbContext.cs
│
├── ViewModels/
│   ├── LoginViewModel.cs
│   ├── UsuarioViewModel.cs
│   ├── GrupoViewModel.cs
│   ├── ProyectoViewModel.cs
│   ├── EvaluacionViewModel.cs
│   └── RankingViewModel.cs
│
├── Services/
│   ├── RankingService.cs
│   └── ArchivoService.cs
│
├── Views/
│   ├── Account/
│   ├── Home/
│   ├── Usuarios/
│   ├── Estudiantes/
│   ├── Profesores/
│   ├── Ferias/
│   ├── Grupos/
│   ├── Proyectos/
│   ├── Tribunales/
│   ├── Evaluaciones/
│   ├── Ranking/
│   ├── Reportes/
│   └── Shared/
│
├── wwwroot/
│   ├── css/
│   ├── js/
│   ├── lib/
│   └── uploads/
│       └── proyectos/
│
├── Migrations/
├── appsettings.json
├── Program.cs
└── AGENTS.md
```

Crear únicamente los archivos que sean necesarios.

Evitar generar clases vacías o archivos sin funcionalidad.

## 5. Base de datos

La aplicación tendrá nueve tablas principales:

1. Usuarios.
2. Estudiantes.
3. Profesores.
4. Ferias.
5. Grupos.
6. GrupoEstudiantes.
7. Proyectos.
8. Tribunales.
9. Evaluaciones.

Utilizar los modelos existentes si ya fueron creados.

### Usuario

Campos principales:

- Id
- Correo
- PasswordHash
- Rol
- Activo

Roles permitidos:

- Administrador
- Profesor
- Estudiante

El correo debe ser único.

### Estudiante

Campos principales:

- Id
- UsuarioId
- Codigo
- Nombre
- Carrera

Cada estudiante estará asociado a un usuario.

El código del estudiante será único.

### Profesor

Campos principales:

- Id
- UsuarioId
- Nombre
- Especialidad

Cada profesor estará asociado a un usuario.

### Feria

Campos principales:

- Id
- Nombre
- FechaInicio
- FechaFin
- Estado

Estados permitidos:

- Abierta
- EnEvaluacion
- Finalizada

### Grupo

Campos principales:

- Id
- Nombre
- FechaCreacion

Los grupos estarán formados por estudiantes.

### GrupoEstudiante

Campos principales:

- Id
- GrupoId
- EstudianteId

No permitir que un estudiante se registre dos veces en el mismo grupo.

### Proyecto

Campos principales:

- Id
- FeriaId
- GrupoId
- Titulo
- Descripcion
- FotoUrl
- PdfUrl
- Estado

Estados permitidos:

- Registrado
- EnEvaluacion
- Evaluado

Un grupo podrá registrar como máximo un proyecto en cada feria.

Un proyecto pertenecerá a una feria y a un grupo.

### Tribunal

Campos principales:

- Id
- ProyectoId
- ProfesorId

Un proyecto podrá tener varios profesores evaluadores.

No permitir asignar dos veces al mismo profesor al mismo proyecto.

### Evaluacion

Campos principales:

- Id
- TribunalId
- Innovacion
- DesarrolloTecnico
- Presentacion
- Observaciones
- FechaEvaluacion

Cada profesor asignado podrá registrar solamente una evaluación por proyecto.

Los criterios tendrán valores entre 0 y 100.

### Reglas para Entity Framework Core

- Utilizar `AppDbContext` existente.
- Configurar correctamente las claves foráneas.
- Utilizar índices únicos cuando corresponda.
- Utilizar `decimal` para las calificaciones.
- Configurar precisión decimal apropiada.
- Evitar eliminaciones en cascada que destruyan evaluaciones históricas.
- Utilizar métodos asincrónicos para consultas y operaciones de escritura.
- Utilizar `AsNoTracking()` cuando solamente se consulten registros.
- No modificar directamente las tablas desde SQL cuando corresponda utilizar migraciones.
- Crear nuevas migraciones solamente cuando existan cambios reales en los modelos.
- No ejecutar migraciones destructivas ni eliminar datos existentes sin autorización.

## 6. Autenticación y autorización

Implementar autenticación mediante cookies de ASP.NET Core.

No utilizar contraseñas en texto plano.

Utilizar `PasswordHasher<Usuario>` para crear y verificar los hashes de las contraseñas.

Crear las siguientes funcionalidades:

- Login.
- Logout.
- Validación de credenciales.
- Gestión de usuarios.
- Control de acceso según roles.
- Redirección después del login.
- Página de acceso denegado.

Configurar correctamente `AddAuthentication()`, `AddCookie()` y el middleware correspondiente en `Program.cs`.

Utilizar claims para identificar al usuario autenticado y su rol.

Utilizar `[Authorize]` y `[Authorize(Roles = "...")]` en los controladores y acciones correspondientes.

No confiar únicamente en ocultar botones o enlaces de las vistas.

Validar los permisos en el servidor para cada operación.

### Administrador

Puede:

- Administrar usuarios.
- Registrar estudiantes.
- Registrar profesores.
- Crear y modificar ferias.
- Consultar grupos.
- Consultar y administrar proyectos.
- Asignar tribunales.
- Consultar evaluaciones.
- Publicar rankings.
- Consultar reportes.

### Profesor

Puede:

- Iniciar sesión.
- Consultar sus proyectos asignados.
- Visualizar la información y documentación de los proyectos.
- Registrar evaluaciones.
- Modificar sus evaluaciones mientras la feria esté en evaluación.
- Consultar resultados publicados.

No puede evaluar proyectos que no le fueron asignados.

### Estudiante

Puede:

- Iniciar sesión.
- Consultar ferias disponibles.
- Crear grupos.
- Incorporar integrantes registrados.
- Consultar sus grupos.
- Registrar proyectos.
- Modificar proyectos mientras se permitan inscripciones.
- Subir fotografías.
- Registrar enlaces a documentos PDF.
- Consultar resultados publicados.

No puede modificar grupos ni proyectos ajenos.

## 7. Administración de usuarios

Crear un CRUD de usuarios exclusivo para administradores.

El formulario incluirá:

- Correo.
- Contraseña.
- Rol.
- Estado activo/inactivo.

La contraseña será obligatoria al crear un usuario.

Al editarlo, la contraseña será opcional. Solamente se actualizará el hash cuando se introduzca una nueva contraseña.

No mostrar nunca el hash de la contraseña en las vistas.

Al crear una cuenta de estudiante o profesor, vincularla con su perfil correspondiente.

Evitar que un mismo usuario tenga simultáneamente perfiles incompatibles.

Implementar una operación inicial para crear el primer administrador si no existe ningún usuario administrador.

El administrador inicial debe configurarse mediante secretos de desarrollo o variables de entorno. No utilizar contraseñas predeterminadas escritas en el código.

## 8. CRUD de estudiantes

Implementar las operaciones:

- Listar estudiantes.
- Registrar estudiante.
- Consultar detalle.
- Editar estudiante.
- Desactivar estudiante o eliminarlo cuando no tenga relaciones que lo impidan.

Campos:

- Código.
- Nombre completo.
- Carrera.
- Correo.
- Contraseña al crear la cuenta.

Utilizar formularios Razor con validaciones.

Agregar búsqueda por nombre o código.

Mostrar mensajes de confirmación después de registrar o editar.

## 9. CRUD de profesores

Implementar:

- Listar profesores.
- Registrar profesor.
- Consultar detalle.
- Editar profesor.
- Desactivar profesor o eliminarlo si no tiene referencias históricas.

Campos:

- Nombre.
- Especialidad.
- Correo.
- Contraseña al crear la cuenta.

Agregar búsqueda por nombre.

Mostrar los proyectos asignados a cada profesor cuando corresponda.

## 10. Gestión de ferias

Implementar CRUD de ferias.

El formulario tendrá:

- Nombre.
- Fecha de inicio.
- Fecha de finalización.
- Estado.

Validar que la fecha de finalización no sea anterior a la fecha de inicio.

Permitir consultar los proyectos registrados en una feria.

Permitir cambiar la feria entre sus estados.

Reglas:

- Abierta: permite registrar proyectos.
- EnEvaluacion: bloquea modificaciones de proyectos y permite registrar calificaciones.
- Finalizada: bloquea evaluaciones y permite publicar resultados oficiales.

No permitir finalizar una feria cuando existan evaluaciones pendientes, salvo que el administrador gestione formalmente las asignaciones correspondientes.

## 11. Gestión de grupos

Los estudiantes podrán formar grupos de trabajo.

Implementar:

- Crear grupo.
- Editar nombre del grupo.
- Consultar integrantes.
- Agregar estudiantes.
- Retirar integrantes.
- Consultar grupos propios.

Un grupo debe tener al menos un estudiante.

El estudiante que crea el grupo será incorporado automáticamente como integrante.

Solamente los integrantes autorizados podrán administrar el grupo.

Para simplificar la primera versión, el estudiante creador será responsable del grupo. Se debe implementar una regla clara para identificarlo sin depender de datos enviados desde el navegador.

No permitir modificar integrantes cuando esto afecte un proyecto cuya feria ya esté en evaluación o finalizada.

Antes de crear o modificar integrantes, validar que no exista una participación incompatible del estudiante en otro grupo de la misma feria.

## 12. Gestión de proyectos

Los proyectos serán registrados por los grupos de estudiantes.

Crear CRUD de proyectos.

El formulario debe tener:

- Feria.
- Grupo.
- Título.
- Descripción.
- Fotografía.
- Enlace del documento PDF.

### Fotografía

Permitir subir una fotografía desde el formulario.

Utilizar `IFormFile`.

Guardar la fotografía en:

`wwwroot/uploads/proyectos/`

Aceptar únicamente formatos de imagen permitidos, como JPG, PNG y WebP, verificando extensión y contenido.

Establecer un tamaño máximo de 5 MB.

Generar nombres únicos para los archivos.

No utilizar el nombre original del archivo como ruta definitiva.

Guardar en MySQL solamente la ruta relativa de la fotografía.

Controlar que los archivos sean imágenes válidas y no confiar únicamente en su extensión.

### Documento PDF

El estudiante proporcionará un enlace externo a su documento PDF.

Ejemplos:

- Google Drive.
- OneDrive.
- Un documento PDF alojado mediante HTTPS.

Guardar el enlace en `PdfUrl`.

Validar que la dirección sea una URL HTTPS válida.

No almacenar el documento PDF físicamente en la base de datos.

Mostrar un enlace para abrir el documento desde la vista de detalle del proyecto.

### Vista de proyectos

Mostrar los proyectos utilizando tarjetas Bootstrap.

Cada tarjeta tendrá:

- Fotografía.
- Título.
- Grupo.
- Feria.
- Descripción breve.
- Estado.
- Enlace para ver detalles.

Agregar filtros por feria y búsqueda por título.

## 13. Asignación de tribunales

El administrador podrá asignar profesores evaluadores a los proyectos.

Implementar:

- Consultar tribunales asignados.
- Asignar profesor a un proyecto.
- Quitar una asignación si todavía no tiene evaluación.
- Consultar evaluadores de un proyecto.

Para la primera versión, cada proyecto deberá contar con tres profesores evaluadores antes de iniciar su evaluación.

No permitir asignaciones duplicadas.

No permitir eliminar una asignación que tenga una evaluación registrada sin un procedimiento explícito de corrección.

Cada profesor solamente podrá consultar y evaluar los proyectos que le correspondan.

## 14. Evaluación de proyectos

Crear una pantalla de evaluación para profesores.

Mostrar:

- Nombre del proyecto.
- Feria.
- Grupo.
- Integrantes.
- Descripción.
- Fotografía.
- Enlace PDF.
- Formulario de calificación.

Los criterios son:

| Criterio | Ponderación |
|---|---:|
| Innovación | 40% |
| Desarrollo técnico | 40% |
| Presentación | 20% |

Las calificaciones estarán comprendidas entre 0 y 100.

Calcular la nota de cada profesor mediante:

`Nota = Innovacion * 0.40 + DesarrolloTecnico * 0.40 + Presentacion * 0.20`

Mostrar la nota calculada en pantalla.

El cálculo definitivo también debe realizarse en el servidor.

No confiar en una nota total enviada desde JavaScript.

Guardar cada criterio y las observaciones en la tabla `Evaluaciones`.

Permitir que cada profesor modifique su propia evaluación mientras la feria esté en estado `EnEvaluacion`.

Impedir que un profesor consulte o modifique evaluaciones ajenas que no esté autorizado a visualizar.

## 15. Ranking de proyectos

Implementar `RankingService`.

Este servicio calculará las calificaciones finales de los proyectos.

La nota final será el promedio de las evaluaciones de los profesores asignados.

`NotaFinal = SumaNotasProfesores / CantidadProfesoresAsignados`

El proyecto solamente entrará en el ranking oficial si todos sus profesores asignados registraron una evaluación y la feria está finalizada.

Ordenar los proyectos de mayor a menor calificación.

Asignar posiciones:

- Primer lugar.
- Segundo lugar.
- Tercer lugar.
- Resto de posiciones.

El ranking será independiente para cada feria.

En caso de empate, utilizar:

1. Mayor promedio de desarrollo técnico.
2. Mayor promedio de innovación.
3. Mayor promedio de presentación.

Si el empate continúa, no asignar ganadores arbitrariamente. Mostrar el empate y permitir que el administrador registre una resolución antes de publicar los resultados.

No almacenar la posición ni la nota final en la tabla `Proyectos` si puede calcularse desde las evaluaciones.

No permitir que los resultados oficiales cambien por modificaciones posteriores a la finalización de la feria.

## 16. Página de resultados

Crear una página pública de resultados por feria.

Mostrar:

- Nombre de la feria.
- Fecha.
- Número de proyectos participantes.
- Primer lugar.
- Segundo lugar.
- Tercer lugar.
- Tabla completa del ranking.

Para los primeros tres lugares, mostrar tarjetas destacadas con la fotografía y el nombre del grupo.

La tabla general incluirá:

- Posición.
- Proyecto.
- Grupo.
- Nota final.
- Cantidad de evaluaciones.

Las ferias abiertas o en evaluación no deben mostrar calificaciones ni rankings parciales al público.

## 17. Dashboard

Crear un dashboard principal para el administrador.

Mostrar tarjetas estadísticas con:

- Total de estudiantes.
- Total de profesores.
- Total de ferias.
- Total de grupos.
- Total de proyectos.
- Evaluaciones pendientes.

Utilizar Chart.js para mostrar gráficos sencillos:

1. Proyectos registrados por feria.
2. Proyectos por estado.
3. Cantidad de proyectos evaluados y pendientes.

Los datos deben obtenerse mediante consultas reales a MySQL.

No escribir números fijos en el dashboard.

El profesor tendrá un panel reducido con sus proyectos asignados, evaluados y pendientes.

El estudiante tendrá un panel con sus grupos, proyectos y ferias disponibles.

## 18. Reportes

Crear `ReportesController`.

Implementar los siguientes reportes:

### Reporte de proyectos por feria

Mostrar:

- Feria.
- Proyecto.
- Grupo.
- Integrantes.
- Estado.

### Reporte de evaluaciones

Mostrar:

- Feria.
- Proyecto.
- Profesor.
- Innovación.
- Desarrollo técnico.
- Presentación.
- Nota calculada.

### Reporte de ranking

Mostrar:

- Feria.
- Posición.
- Proyecto.
- Grupo.
- Nota final.

### Reporte de profesores

Mostrar:

- Profesor.
- Cantidad de proyectos asignados.
- Cantidad de evaluaciones realizadas.
- Evaluaciones pendientes.

### Exportación

La primera versión debe permitir imprimir los reportes mediante vistas HTML optimizadas para impresión.

Agregar exportación CSV cuando sea útil.

No incorporar inicialmente un motor externo de reportes ni librerías complejas de exportación.

## 19. Diseño de interfaz

Utilizar Bootstrap 5.

La interfaz debe ser limpia, profesional, responsiva y consistente.

Características:

- Barra de navegación superior.
- Menú de opciones según el rol.
- Dashboard con tarjetas.
- Tablas Bootstrap para los CRUD.
- Formularios sencillos.
- Botones para crear, editar, consultar y eliminar.
- Confirmación antes de eliminar.
- Mensajes de éxito y error.
- Validación visual de formularios.
- Fotografías en tarjetas de proyectos.
- Tablas con filtros y búsqueda.

Utilizar colores sobrios:

- Azul oscuro como color principal.
- Blanco para el fondo.
- Gris claro para secciones.
- Verde para operaciones exitosas.
- Rojo para errores.
- Dorado, plateado y bronce únicamente para representar los tres primeros lugares.

Evitar animaciones innecesarias.

No incorporar plantillas administrativas excesivamente complejas.

Crear un diseño compartido utilizando `_Layout.cshtml`.

## 20. Validaciones y seguridad

Aplicar las siguientes reglas:

- Validar todos los formularios mediante `ModelState`.
- Utilizar ViewModels para recibir información de formularios.
- No enlazar directamente propiedades sensibles como `PasswordHash`.
- Evitar asignación masiva de propiedades no autorizadas.
- Utilizar tokens antifalsificación en operaciones POST.
- Aplicar autorización en controladores y acciones.
- Validar en el servidor la propiedad de los proyectos y grupos.
- No mostrar errores internos de base de datos al usuario final.
- No registrar contraseñas en logs.
- No almacenar credenciales reales en el repositorio.
- Validar imágenes antes de guardarlas.
- Validar los enlaces externos.
- No construir consultas SQL concatenando valores ingresados por usuarios.
- Usar LINQ o consultas parametrizadas.
- Utilizar consultas asincrónicas.
- Comprobar resultados antes de utilizar entidades relacionadas.
- Mostrar páginas 404 o acceso denegado cuando corresponda.

## 21. Reglas para escribir código

Mantener las siguientes convenciones:

1. Utilizar nombres descriptivos en español para entidades y propiedades del dominio.
2. Mantener los nombres propios de las APIs y convenciones técnicas de .NET.
3. Utilizar PascalCase en clases, métodos y propiedades.
4. Utilizar camelCase en variables locales.
5. Mantener los controladores con responsabilidades claras.
6. Evitar duplicar consultas y cálculos.
7. Utilizar servicios solamente para lógica que justifique su separación, como ranking o archivos.
8. No introducir abstracciones que no aporten funcionalidad.
9. Mantener los métodos cortos cuando sea razonable.
10. Añadir comentarios únicamente cuando expliquen decisiones técnicas o reglas de negocio no evidentes.
11. No agregar comentarios que repitan literalmente lo que hace el código.
12. No introducir dependencias NuGet innecesarias.
13. Conservar el estilo del proyecto existente.
14. No modificar archivos ajenos a la funcionalidad implementada.
15. Comprobar que el código compile después de cada etapa.

No utilizar contenido de ejemplo o valores fijos como sustitutos de consultas reales en las funcionalidades definitivas.

## 22. Orden obligatorio de implementación

Desarrollar el sistema siguiendo estas etapas:

### Etapa 1. Verificación y base de datos

- Inspeccionar el proyecto existente.
- Revisar modelos y relaciones.
- Verificar la conexión con MySQL.
- Corregir problemas de configuración si existen.
- Crear las migraciones pendientes cuando corresponda.
- Comprobar la compilación.

### Etapa 2. Autenticación

- Configurar cookies.
- Implementar login.
- Implementar logout.
- Implementar autorización por rol.
- Implementar creación segura del administrador inicial.
- Crear las vistas de autenticación.

### Etapa 3. Administración

- CRUD de usuarios.
- CRUD de estudiantes.
- CRUD de profesores.
- CRUD de ferias.

### Etapa 4. Grupos y proyectos

- Gestión de grupos.
- Gestión de integrantes.
- CRUD de proyectos.
- Carga de fotografías.
- Enlaces PDF.
- Filtros y búsquedas.

### Etapa 5. Tribunales y calificaciones

- Asignación de profesores.
- Consulta de proyectos asignados.
- Formulario de evaluación.
- Cálculo de calificaciones.
- Validación de evaluaciones pendientes.

### Etapa 6. Ranking y reportes

- Implementar `RankingService`.
- Calcular notas finales.
- Crear ranking por feria.
- Mostrar ganadores.
- Implementar reportes.
- Implementar dashboard.

### Etapa 7. Verificación final

- Ejecutar `dotnet build`.
- Revisar rutas y vistas Razor.
- Revisar permisos.
- Comprobar los CRUD.
- Comprobar las relaciones.
- Verificar cálculo del ranking.
- Revisar validaciones.
- Revisar el manejo de errores.

## 23. Estrategia de trabajo para Codex

El desarrollo debe ser incremental.

Antes de implementar cada etapa:

1. Revisar qué archivos ya existen.
2. Identificar qué funcionalidades están completas.
3. Determinar los archivos que necesitan modificaciones.
4. Implementar la funcionalidad correspondiente.
5. Compilar el proyecto.
6. Corregir los errores de compilación.
7. Informar qué archivos fueron creados o modificados.
8. Resumir cómo probar manualmente la funcionalidad.

No volver a implementar funcionalidades que ya estén terminadas y operativas.

No reemplazar código funcional solamente para cambiar su estilo.

No ejecutar comandos que eliminen o reinicien la base de datos.

No utilizar `dotnet ef database drop`.

No eliminar migraciones aplicadas.

No modificar credenciales ni configuración de conexión existente sin necesidad.

Si una operación requiere una decisión que pueda destruir información o alterar los datos existentes, detener esa operación y explicarla.

## 24. Criterios de aceptación

El sistema estará terminado cuando cumpla lo siguiente:

- El administrador puede iniciar sesión.
- El administrador puede crear usuarios estudiantes y profesores.
- Los estudiantes pueden iniciar sesión.
- Los profesores pueden iniciar sesión.
- El administrador puede registrar ferias.
- Los estudiantes pueden formar grupos.
- Los grupos pueden registrar proyectos.
- Los proyectos pueden tener fotografía y enlace PDF.
- El administrador puede asignar tres profesores a un proyecto.
- Los profesores pueden evaluar únicamente sus proyectos asignados.
- Las notas se calculan correctamente.
- El sistema impide evaluaciones duplicadas.
- El sistema identifica las evaluaciones pendientes.
- Una feria puede finalizarse cuando todas las evaluaciones están completas.
- El ranking se genera de forma automática.
- Se muestran los tres primeros lugares.
- Los reportes consultan datos reales.
- Cada rol accede exclusivamente a las funcionalidades autorizadas.
- Las operaciones principales persisten correctamente en MySQL.
- El proyecto compila sin errores.

## 25. Instrucción principal

Desarrollar FeriaPro respetando todas las especificaciones anteriores.

Priorizar la funcionalidad, la claridad del código y la integridad de la base de datos por encima de arquitecturas sofisticadas.

Trabajar sobre el proyecto existente sin recrearlo.

Utilizar Entity Framework Core y MySQL de XAMPP como mecanismo de persistencia.

Implementar las funcionalidades completas, incluyendo controladores, vistas, validaciones, consultas y persistencia.

Avanzar en el orden indicado, comprobando la compilación y el funcionamiento de cada etapa antes de continuar.

No considerar completa una funcionalidad que solamente tenga la interfaz visual sin conexión a la base de datos.