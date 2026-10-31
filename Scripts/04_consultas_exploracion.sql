-- ==============================================================================
-- Proyecto Primer Parcial: Mini Sistema de Renta de Películas Sakila
-- Estudiante: Héctor Robles
-- Script 04: Consultas de Exploración Inicial de la Base de Datos Sakila
-- ==============================================================================
USE sakila;
GO

-- ==============================================================================
-- Consulta 1 (Enunciado 1 de la lista):
-- "Muestre las 10 películas con mayor cantidad de alquileres, indicando título y número de alquileres."
-- ==============================================================================
-- Explicación:
-- Esta consulta une las tablas film, inventory y rental mediante INNER JOIN.
-- Agrupa por identificador y título de la película y cuenta las transacciones de alquiler.
-- Se ordenan en forma descendente para obtener el TOP 10 de películas más demandadas.

SELECT TOP 10 
    f.film_id, 
    f.title AS pelicula, 
    COUNT(r.rental_id) AS total_alquileres
FROM film f
INNER JOIN inventory i ON f.film_id = i.film_id
INNER JOIN rental r ON i.inventory_id = r.inventory_id
GROUP BY f.film_id, f.title
ORDER BY total_alquileres DESC;
GO

-- ==============================================================================
-- Consulta 2 (Enunciado 2 de la lista):
-- "Muestre los 5 clientes que hayan realizado más alquileres, indicando nombre completo y cantidad total."
-- ==============================================================================
-- Explicación:
-- Esta consulta vincula la tabla customer con rental mediante INNER JOIN.
-- Concatena first_name y last_name para formar el nombre completo del cliente,
-- agrupa los registros y calcula el conteo total de alquileres realizados por cada cliente,
-- presentando los 5 clientes más frecuentes del videoclub.

SELECT TOP 5 
    c.customer_id, 
    CONCAT(c.first_name, ' ', c.last_name) AS nombre_completo,
    c.email,
    COUNT(r.rental_id) AS total_alquileres
FROM customer c
INNER JOIN rental r ON c.customer_id = r.customer_id
GROUP BY c.customer_id, c.first_name, c.last_name, c.email
ORDER BY total_alquileres DESC;
GO

-- ==============================================================================
-- Consulta 3 (Enunciado 3 de la lista):
-- "Muestre las 10 películas que actualmente tengan al menos una copia disponible para alquilar."
-- ==============================================================================
-- Explicación:
-- Una copia en inventario está disponible si NO se encuentra en la tabla rental
-- con return_date IS NULL (es decir, en préstamo activo sin devolver).
-- La subconsulta filtra aquellas copias que no tienen devoluciones pendientes.
-- Se agrupa por película para mostrar cuántas copias reales disponibles tiene cada una.

SELECT TOP 10 
    f.film_id, 
    f.title AS pelicula, 
    f.rating,
    COUNT(DISTINCT i.inventory_id) AS copias_disponibles
FROM film f
INNER JOIN inventory i ON f.film_id = i.film_id
WHERE i.inventory_id NOT IN (
    SELECT r.inventory_id 
    FROM rental r 
    WHERE r.return_date IS NULL
)
GROUP BY f.film_id, f.title, f.rating
ORDER BY f.title ASC;
GO
