-- ==============================================================================
-- PROYECTO PRIMER PARCIAL: MINI SISTEMA DE RENTA DE PELÍCULAS SAKILA
-- ESTUDIANTE: Héctor Robles
-- SCRIPT DE CONSULTAS PARA EL VIDEO DEMOSTRATIVO EN SQL SERVER
-- ==============================================================================
USE sakila;
GO

-- ==============================================================================
-- PASO 1 (Video): RETO 1 - EVIDENCIA DE CONTRASEÑAS PROTEGIDAS CON PBKDF2 Y SAL
-- Muestra que las contraseñas NO se guardan en texto plano en la base de datos.
-- ==============================================================================
SELECT 
    usuario_id, 
    username, 
    rol, 
    password_hash, 
    salt, 
    nombre_completo, 
    customer_id, 
    staff_id, 
    activo
FROM usuarios;
GO

-- ==============================================================================
-- PASO 2 (Video): RELACIÓN FUNDAMENTAL: film -> inventory -> rental -> customer
-- Evidencia de las tablas del modelo relacional utilizadas en la aplicación.
-- ==============================================================================
-- 1. Películas en catálogo
SELECT TOP 5 film_id, title, rating, rental_rate, rental_duration, is_deleted 
FROM film 
ORDER BY film_id ASC;

-- 2. Copias físicas de inventario para una película (ej. Film #1)
SELECT inventory_id, film_id, store_id, last_update 
FROM inventory 
WHERE film_id = 1;

-- 3. Últimos alquileres registrados con detalle del cliente y película
SELECT TOP 5 
    r.rental_id, 
    r.rental_date, 
    r.return_date, 
    f.title AS pelicula, 
    CONCAT(c.first_name, ' ', c.last_name) AS cliente
FROM rental r
INNER JOIN inventory i ON r.inventory_id = i.inventory_id
INNER JOIN film f ON i.film_id = f.film_id
INNER JOIN customer c ON r.customer_id = c.customer_id
ORDER BY r.rental_id DESC;
GO

-- ==============================================================================
-- PASO 3 (Video): COMPROBACIÓN DE ALQUILER Y DEVOLUCIÓN EN TIEMPO REAL
-- Ejecutar esta consulta después de presionar "Alquilar" en la web para ver
-- el nuevo registro con return_date NULL.
-- Luego, volver a ejecutarla tras presionar "Devolver" para ver la fecha asignada.
-- ==============================================================================
SELECT TOP 1 
    r.rental_id, 
    r.rental_date, 
    r.return_date, 
    f.title AS pelicula, 
    r.inventory_id,
    CONCAT(c.first_name, ' ', c.last_name) AS cliente,
    CASE WHEN r.return_date IS NULL THEN 'EN PRÉSTAMO (ACTIVO)' ELSE 'DEVUELTO' END AS estado
FROM rental r
INNER JOIN inventory i ON r.inventory_id = i.inventory_id
INNER JOIN film f ON i.film_id = f.film_id
INNER JOIN customer c ON r.customer_id = c.customer_id
ORDER BY r.rental_id DESC;
GO

-- ==============================================================================
-- PASO 4 (Video): RETO 2 - EVIDENCIA DE ELIMINACIÓN LÓGICA (film.is_deleted)
-- Muestra el valor de 'is_deleted' de una película (ej. Film #1: ACADEMY DINOSAUR).
-- Ejecutar cuando la película esté activa (is_deleted = 0) y tras eliminarla (is_deleted = 1).
-- ==============================================================================
SELECT 
    film_id, 
    title, 
    is_deleted, 
    CASE WHEN is_deleted = 1 THEN 'ELIMINADA LÓGICAMENTE (Inactiva)' ELSE 'ACTIVA (Visible en catálogo)' END AS estado_logico,
    last_update
FROM film 
WHERE film_id = 1;
GO

-- ==============================================================================
-- PASO 5 (Video): REPORTE TOP 10 PELÍCULAS MÁS ALQUILADAS
-- Consulta exacta ejecutada por ADO.NET en el Controller para la vista del reporte.
-- Demuestra el uso de INNER JOIN, COUNT, GROUP BY, ORDER BY y TOP (10).
-- ==============================================================================
SELECT TOP 10 
    f.film_id, 
    f.title AS pelicula, 
    COUNT(r.rental_id) AS total_alquileres,
    f.rating,
    f.rental_rate,
    (SELECT COUNT(*) FROM inventory i WHERE i.film_id = f.film_id) AS copias_inventario,
    (SELECT COUNT(*) FROM inventory i WHERE i.film_id = f.film_id AND i.inventory_id NOT IN (
        SELECT r2.inventory_id FROM rental r2 WHERE r2.return_date IS NULL
    )) AS copias_disponibles
FROM film f
INNER JOIN inventory i ON f.film_id = i.film_id
INNER JOIN rental r ON i.inventory_id = r.inventory_id
GROUP BY f.film_id, f.title, f.rating, f.rental_rate
ORDER BY total_alquileres DESC;
GO
