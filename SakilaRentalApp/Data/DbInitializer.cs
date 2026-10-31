using Microsoft.Data.SqlClient;
using SakilaRentalApp.Utils;
using System.Data;

namespace SakilaRentalApp.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(DatabaseHelper db, ILogger logger)
        {
            try
            {
                using var conn = await db.GetOpenConnectionAsync();

                // 1. Asegurar columna is_deleted en film (Reto 2: Eliminación Lógica)
                const string alterFilmSql = @"
                    IF NOT EXISTS (
                        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
                        WHERE TABLE_NAME = 'film' AND COLUMN_NAME = 'is_deleted'
                    )
                    BEGIN
                        ALTER TABLE film ADD is_deleted BIT NOT NULL CONSTRAINT DF_film_is_deleted DEFAULT 0;
                    END";

                using (var cmd = new SqlCommand(alterFilmSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 2. Asegurar tabla usuarios (Reto 1: Autenticación con contraseña protegida)
                const string createUsuariosSql = @"
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
                            rol VARCHAR(20) NOT NULL,
                            nombre_completo VARCHAR(100) NOT NULL,
                            email VARCHAR(100) NULL,
                            customer_id INT NULL,
                            staff_id TINYINT NULL,
                            activo BIT NOT NULL DEFAULT 1,
                            fecha_creacion DATETIME NOT NULL DEFAULT GETDATE(),
                            CONSTRAINT FK_usuarios_customer FOREIGN KEY (customer_id) REFERENCES customer(customer_id),
                            CONSTRAINT FK_usuarios_staff FOREIGN KEY (staff_id) REFERENCES staff(staff_id)
                        );
                    END";

                using (var cmd = new SqlCommand(createUsuariosSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // 3. Sembrar usuarios iniciales si no existen
                await SeedUserAsync(conn, "admin", "Admin123!", "Administrador", "Mike Hillyer (Admin)", "Mike.Hillyer@sakilastaff.com", null, 1);
                await SeedUserAsync(conn, "mary.smith", "Cliente123!", "Cliente", "MARY SMITH", "MARY.SMITH@sakilacustomer.org", 1, null);
                await SeedUserAsync(conn, "patricia.johnson", "Cliente123!", "Cliente", "PATRICIA JOHNSON", "PATRICIA.JOHNSON@sakilacustomer.org", 2, null);

                logger.LogInformation("Base de datos Sakila inicializada y verificada correctamente.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error al inicializar la base de datos Sakila.");
            }
        }

        private static async Task SeedUserAsync(
            SqlConnection conn,
            string username,
            string plainPassword,
            string rol,
            string nombreCompleto,
            string email,
            int? customerId,
            byte? staffId)
        {
            const string checkSql = "SELECT COUNT(*) FROM usuarios WHERE username = @Username;";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username;

            int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
            if (count == 0)
            {
                var (hash, salt) = PasswordHelper.HashPassword(plainPassword);

                const string insertSql = @"
                    INSERT INTO usuarios (username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion)
                    VALUES (@Username, @PasswordHash, @Salt, @Rol, @NombreCompleto, @Email, @CustomerId, @StaffId, 1, GETDATE());";

                using var insertCmd = new SqlCommand(insertSql, conn);
                insertCmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username;
                insertCmd.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 256).Value = hash;
                insertCmd.Parameters.Add("@Salt", SqlDbType.VarChar, 256).Value = salt;
                insertCmd.Parameters.Add("@Rol", SqlDbType.VarChar, 20).Value = rol;
                insertCmd.Parameters.Add("@NombreCompleto", SqlDbType.VarChar, 100).Value = nombreCompleto;
                insertCmd.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = (object?)email ?? DBNull.Value;
                insertCmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = (object?)customerId ?? DBNull.Value;
                insertCmd.Parameters.Add("@StaffId", SqlDbType.TinyInt).Value = (object?)staffId ?? DBNull.Value;

                await insertCmd.ExecuteNonQueryAsync();
            }
        }
    }
}
