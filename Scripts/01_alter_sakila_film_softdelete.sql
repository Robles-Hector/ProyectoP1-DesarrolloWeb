-- ==============================================================================
-- Proyecto Primer Parcial: Mini Sistema de Renta de Películas Sakila
-- Estudiante: Héctor Robles
-- Script 01: Eliminación Lógica para la entidad Film
-- ==============================================================================
USE sakila;
GO

-- Reto Técnico 2: Eliminación Lógica
-- Se agrega la columna 'is_deleted' a la tabla 'film' con valor por defecto 0 (Activo).
-- Justificación:
-- Una película no debe eliminarse físicamente de la base de datos porque rompería
-- la integridad referencial con los inventarios históricos (inventory) y los
-- alquileres pasados (rental), arruinando los registros de facturación, métricas y auditorías.
-- Con la eliminación lógica, la película permanece para fines históricos pero no se
-- mostrará en el catálogo ni estará disponible para nuevos alquileres.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'film' AND COLUMN_NAME = 'is_deleted'
)
BEGIN
    ALTER TABLE film ADD is_deleted BIT NOT NULL CONSTRAINT DF_film_is_deleted DEFAULT 0;
    PRINT 'Columna is_deleted agregada exitosamente a la tabla film.';
END
ELSE
BEGIN
    PRINT 'La columna is_deleted ya existe en la tabla film.';
END
GO
