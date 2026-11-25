CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;
ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `Usuarios` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Correo` varchar(254) CHARACTER SET utf8mb4 NOT NULL,
    `PasswordHash` longtext CHARACTER SET utf8mb4 NOT NULL,
    `Rol` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `Activo` tinyint(1) NOT NULL,
    CONSTRAINT `PK_Usuarios` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Estudiantes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UsuarioId` int NOT NULL,
    `Codigo` varchar(40) CHARACTER SET utf8mb4 NOT NULL,
    `Nombre` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Carrera` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Estudiantes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Estudiantes_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Ferias` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `FechaInicio` datetime(6) NOT NULL,
    `FechaFin` datetime(6) NOT NULL,
    `Estado` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    `ResultadosPublicados` tinyint(1) NOT NULL,
    `OrdenDesempate` longtext CHARACTER SET utf8mb4 NULL,
    `ResolucionEmpate` longtext CHARACTER SET utf8mb4 NULL,
    `PublicadoPorUsuarioId` int NULL,
    `FechaPublicacion` datetime(6) NULL,
    CONSTRAINT `PK_Ferias` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Ferias_Usuarios_PublicadoPorUsuarioId` FOREIGN KEY (`PublicadoPorUsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Profesores` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `UsuarioId` int NOT NULL,
    `Nombre` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `Especialidad` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Profesores` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Profesores_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `Usuarios` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Grupos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `Nombre` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `FechaCreacion` datetime(6) NOT NULL,
    `ResponsableEstudianteId` int NOT NULL,
    CONSTRAINT `PK_Grupos` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Grupos_Estudiantes_ResponsableEstudianteId` FOREIGN KEY (`ResponsableEstudianteId`) REFERENCES `Estudiantes` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `GrupoEstudiantes` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `GrupoId` int NOT NULL,
    `EstudianteId` int NOT NULL,
    CONSTRAINT `PK_GrupoEstudiantes` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_GrupoEstudiantes_Estudiantes_EstudianteId` FOREIGN KEY (`EstudianteId`) REFERENCES `Estudiantes` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_GrupoEstudiantes_Grupos_GrupoId` FOREIGN KEY (`GrupoId`) REFERENCES `Grupos` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Proyectos` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `FeriaId` int NOT NULL,
    `GrupoId` int NOT NULL,
    `Titulo` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Descripcion` varchar(5000) CHARACTER SET utf8mb4 NOT NULL,
    `FotoUrl` varchar(300) CHARACTER SET utf8mb4 NULL,
    `PdfUrl` varchar(2048) CHARACTER SET utf8mb4 NOT NULL,
    `Estado` varchar(20) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Proyectos` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Proyectos_Ferias_FeriaId` FOREIGN KEY (`FeriaId`) REFERENCES `Ferias` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Proyectos_Grupos_GrupoId` FOREIGN KEY (`GrupoId`) REFERENCES `Grupos` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Tribunales` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `ProyectoId` int NOT NULL,
    `ProfesorId` int NOT NULL,
    CONSTRAINT `PK_Tribunales` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Tribunales_Profesores_ProfesorId` FOREIGN KEY (`ProfesorId`) REFERENCES `Profesores` (`Id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_Tribunales_Proyectos_ProyectoId` FOREIGN KEY (`ProyectoId`) REFERENCES `Proyectos` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `Evaluaciones` (
    `Id` int NOT NULL AUTO_INCREMENT,
    `TribunalId` int NOT NULL,
    `Innovacion` decimal(5,2) NOT NULL,
    `DesarrolloTecnico` decimal(5,2) NOT NULL,
    `Presentacion` decimal(5,2) NOT NULL,
    `Observaciones` varchar(2000) CHARACTER SET utf8mb4 NULL,
    `FechaEvaluacion` datetime(6) NOT NULL,
    CONSTRAINT `PK_Evaluaciones` PRIMARY KEY (`Id`),
    CONSTRAINT `FK_Evaluaciones_Tribunales_TribunalId` FOREIGN KEY (`TribunalId`) REFERENCES `Tribunales` (`Id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_Estudiantes_Codigo` ON `Estudiantes` (`Codigo`);

CREATE UNIQUE INDEX `IX_Estudiantes_UsuarioId` ON `Estudiantes` (`UsuarioId`);

CREATE UNIQUE INDEX `IX_Evaluaciones_TribunalId` ON `Evaluaciones` (`TribunalId`);

CREATE INDEX `IX_Ferias_PublicadoPorUsuarioId` ON `Ferias` (`PublicadoPorUsuarioId`);

CREATE INDEX `IX_GrupoEstudiantes_EstudianteId` ON `GrupoEstudiantes` (`EstudianteId`);

CREATE UNIQUE INDEX `IX_GrupoEstudiantes_GrupoId_EstudianteId` ON `GrupoEstudiantes` (`GrupoId`, `EstudianteId`);

CREATE INDEX `IX_Grupos_ResponsableEstudianteId` ON `Grupos` (`ResponsableEstudianteId`);

CREATE UNIQUE INDEX `IX_Profesores_UsuarioId` ON `Profesores` (`UsuarioId`);

CREATE UNIQUE INDEX `IX_Proyectos_FeriaId_GrupoId` ON `Proyectos` (`FeriaId`, `GrupoId`);

CREATE INDEX `IX_Proyectos_GrupoId` ON `Proyectos` (`GrupoId`);

CREATE INDEX `IX_Tribunales_ProfesorId` ON `Tribunales` (`ProfesorId`);

CREATE UNIQUE INDEX `IX_Tribunales_ProyectoId_ProfesorId` ON `Tribunales` (`ProyectoId`, `ProfesorId`);

CREATE UNIQUE INDEX `IX_Usuarios_Correo` ON `Usuarios` (`Correo`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261118043003_InicialFeriaPro', '9.0.22');

COMMIT;

