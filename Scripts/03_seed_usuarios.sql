-- ==============================================================================
-- Proyecto Primer Parcial: Mini Sistema de Renta de Películas Sakila
-- Estudiante: Héctor Robles
-- Script 03: Población de Usuarios Iniciales con Contraseñas Criptográficas
-- ==============================================================================
USE sakila;
GO

-- Reto 1: Protección de Contraseñas
-- Las contraseñas han sido generadas mediante PBKDF2 (Rfc2898DeriveBytes) con HMAC-SHA256,
-- utilizando 100,000 iteraciones y una sal criptográfica única de 128 bits (16 bytes).
-- 
-- Credenciales de Acceso:
-- 1. Administrador:
--    Usuario:    admin
--    Contraseña: Admin123!
--    Asociado a: staff_id = 1 (Mike Hillyer)
--
-- 2. Cliente 1:
--    Usuario:    mary.smith
--    Contraseña: Cliente123!
--    Asociado a: customer_id = 1 (MARY SMITH)
--
-- 3. Cliente 2:
--    Usuario:    patricia.johnson
--    Contraseña: Cliente123!
--    Asociado a: customer_id = 2 (PATRICIA JOHNSON)

IF NOT EXISTS (SELECT 1 FROM usuarios WHERE username = 'admin')
BEGIN
    INSERT INTO usuarios (username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion)
    VALUES (
        'admin',
        'Ua4cz0ia67UoQezEWHW0O9NL/y5HBUSBBNN/p3az3M4=',
        'UWnDP8kBp9i/rv/SbWFS+g==',
        'Administrador',
        'Mike Hillyer (Admin)',
        'Mike.Hillyer@sakilastaff.com',
        NULL,
        1,
        1,
        GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM usuarios WHERE username = 'mary.smith')
BEGIN
    INSERT INTO usuarios (username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion)
    VALUES (
        'mary.smith',
        'ZF/WrVM/BfoUuhLfIPEQvbu21v0kyj9f+B4W8K+an6w=',
        'jMc6f/lxK+Kpqnqoggiqgg==',
        'Cliente',
        'MARY SMITH',
        'MARY.SMITH@sakilacustomer.org',
        1,
        NULL,
        1,
        GETDATE()
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM usuarios WHERE username = 'patricia.johnson')
BEGIN
    INSERT INTO usuarios (username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion)
    VALUES (
        'patricia.johnson',
        'r2pKlj9nvYqF3JOHqpWnAiRPTC87Cl6n3vAxlEdoK1Q=',
        'IkAcdVxHd53MtkJrmHQrdw==',
        'Cliente',
        'PATRICIA JOHNSON',
        'PATRICIA.JOHNSON@sakilacustomer.org',
        2,
        NULL,
        1,
        GETDATE()
    );
END
GO

PRINT 'Usuarios iniciales verificados e insertados exitosamente.';
GO
