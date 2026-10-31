-- ==============================================================================
-- Proyecto Primer Parcial: Mini Sistema de Renta de Películas Sakila
-- Estudiante: Héctor Robles
-- Script 02: Creación de la Tabla de Usuarios y Autenticación
-- ==============================================================================
USE sakila;
GO

-- Creación de la tabla 'usuarios' para administrar la autenticación y autorización
-- Permite asociar la cuenta a un cliente (customer_id) o a un empleado (staff_id).
-- Reto 1: Se almacena el hash de la contraseña y una sal criptográfica (salt),
-- evitando almacenar contraseñas en texto plano.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.TABLES 
    WHERE TABLE_NAME = 'usuarios'
)
BEGIN
    CREATE TABLE usuarios (
        usuario_id INT IDENTITY(1,1) PRIMARY KEY,
        username VARCHAR(50) NOT NULL UNIQUE,
        password_hash VARCHAR(256) NOT NULL,
        salt VARCHAR(256) NOT NULL,
        rol VARCHAR(20) NOT NULL, -- 'Administrador', 'Cliente'
        nombre_completo VARCHAR(100) NOT NULL,
        email VARCHAR(100) NULL,
        customer_id INT NULL,
        staff_id TINYINT NULL,
        activo BIT NOT NULL DEFAULT 1,
        fecha_creacion DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_usuarios_customer FOREIGN KEY (customer_id) REFERENCES customer(customer_id),
        CONSTRAINT FK_usuarios_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
    );
    PRINT 'Tabla usuarios creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla usuarios ya existe.';
END
GO
