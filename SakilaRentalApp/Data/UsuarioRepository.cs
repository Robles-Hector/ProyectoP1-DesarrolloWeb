using Microsoft.Data.SqlClient;
using SakilaRentalApp.Models;
using SakilaRentalApp.Utils;
using System.Data;

namespace SakilaRentalApp.Data
{
    public class UsuarioRepository
    {
        private readonly DatabaseHelper _db;

        public UsuarioRepository(DatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Valida las credenciales de un usuario comparando la sal y hash criptográfico PBKDF2.
        /// </summary>
        public async Task<Usuario?> ValidarLoginAsync(string username, string password)
        {
            const string sql = @"
                SELECT usuario_id, username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion
                FROM usuarios
                WHERE username = @Username AND activo = 1;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username.Trim();

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                string storedHash = reader.GetString(reader.GetOrdinal("password_hash"));
                string storedSalt = reader.GetString(reader.GetOrdinal("salt"));

                // Reto 1: Verificación de contraseña usando el hash y salt
                bool passwordValida = PasswordHelper.VerifyPassword(password, storedHash, storedSalt);
                if (!passwordValida)
                {
                    return null;
                }

                return MapUsuario(reader);
            }

            return null;
        }

        public async Task<Usuario?> GetByUsernameAsync(string username)
        {
            const string sql = @"
                SELECT usuario_id, username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion
                FROM usuarios
                WHERE username = @Username;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username.Trim();

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUsuario(reader);
            }
            return null;
        }

        public async Task<Usuario?> GetByIdAsync(int usuarioId)
        {
            const string sql = @"
                SELECT usuario_id, username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion
                FROM usuarios
                WHERE usuario_id = @UsuarioId;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@UsuarioId", SqlDbType.Int).Value = usuarioId;

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUsuario(reader);
            }
            return null;
        }

        /// <summary>
        /// Registra un nuevo usuario en la base de datos aplicando Hashing con Sal.
        /// Si es cliente, asegura que exista su registro en la tabla 'customer' de Sakila.
        /// </summary>
        public async Task<(bool Success, string Message, Usuario? Usuario)> RegistrarClienteAsync(
            string username, string password, string firstName, string lastName, string email)
        {
            using var conn = await _db.GetOpenConnectionAsync();
            using var tx = conn.BeginTransaction();

            try
            {
                // 1. Verificar si el username ya existe
                const string checkSql = "SELECT COUNT(*) FROM usuarios WHERE username = @Username;";
                using (var checkCmd = new SqlCommand(checkSql, conn, tx))
                {
                    checkCmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username.Trim();
                    int count = (int)(await checkCmd.ExecuteScalarAsync() ?? 0);
                    if (count > 0)
                    {
                        return (false, "El nombre de usuario ya se encuentra registrado.", null);
                    }
                }

                // 2. Crear o asociar registro en tabla customer de Sakila
                int customerId;
                const string checkCustomerSql = "SELECT TOP 1 customer_id FROM customer WHERE email = @Email;";
                using (var checkCustCmd = new SqlCommand(checkCustomerSql, conn, tx))
                {
                    checkCustCmd.Parameters.Add("@Email", SqlDbType.VarChar, 50).Value = email.Trim();
                    var existingId = await checkCustCmd.ExecuteScalarAsync();
                    if (existingId != null && existingId != DBNull.Value)
                    {
                        customerId = Convert.ToInt32(existingId);
                    }
                    else
                    {
                        // Insertar nuevo customer en Sakila (store_id=1, address_id=1)
                        const string insertCustSql = @"
                            INSERT INTO customer (store_id, first_name, last_name, email, address_id, active, create_date, last_update)
                            VALUES (1, @FirstName, @LastName, @Email, 1, '1', GETDATE(), GETDATE());
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        using var insertCustCmd = new SqlCommand(insertCustSql, conn, tx);
                        insertCustCmd.Parameters.Add("@FirstName", SqlDbType.VarChar, 45).Value = firstName.Trim().ToUpper();
                        insertCustCmd.Parameters.Add("@LastName", SqlDbType.VarChar, 45).Value = lastName.Trim().ToUpper();
                        insertCustCmd.Parameters.Add("@Email", SqlDbType.VarChar, 50).Value = email.Trim();
                        customerId = (int)(await insertCustCmd.ExecuteScalarAsync() ?? 0);
                    }
                }

                // 3. Generar Hash y Salt para la contraseña
                var (hash, salt) = PasswordHelper.HashPassword(password);
                string nombreCompleto = $"{firstName.Trim()} {lastName.Trim()}";

                // 4. Insertar en tabla usuarios
                const string insertUserSql = @"
                    INSERT INTO usuarios (username, password_hash, salt, rol, nombre_completo, email, customer_id, staff_id, activo, fecha_creacion)
                    VALUES (@Username, @PasswordHash, @Salt, 'Cliente', @NombreCompleto, @Email, @CustomerId, NULL, 1, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int usuarioId;
                using (var insertUserCmd = new SqlCommand(insertUserSql, conn, tx))
                {
                    insertUserCmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = username.Trim();
                    insertUserCmd.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 256).Value = hash;
                    insertUserCmd.Parameters.Add("@Salt", SqlDbType.VarChar, 256).Value = salt;
                    insertUserCmd.Parameters.Add("@NombreCompleto", SqlDbType.VarChar, 100).Value = nombreCompleto;
                    insertUserCmd.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = email.Trim();
                    insertUserCmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

                    usuarioId = (int)(await insertUserCmd.ExecuteScalarAsync() ?? 0);
                }

                await tx.CommitAsync();

                var nuevoUsuario = new Usuario
                {
                    UsuarioId = usuarioId,
                    Username = username.Trim(),
                    Rol = "Cliente",
                    NombreCompleto = nombreCompleto,
                    Email = email.Trim(),
                    CustomerId = customerId,
                    Activo = true,
                    FechaCreacion = DateTime.Now
                };

                return (true, "Usuario registrado exitosamente.", nuevoUsuario);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al registrar usuario: {ex.Message}", null);
            }
        }

        private static Usuario MapUsuario(SqlDataReader reader)
        {
            return new Usuario
            {
                UsuarioId = reader.GetInt32(reader.GetOrdinal("usuario_id")),
                Username = reader.GetString(reader.GetOrdinal("username")),
                PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                Salt = reader.GetString(reader.GetOrdinal("salt")),
                Rol = reader.GetString(reader.GetOrdinal("rol")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("nombre_completo")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                CustomerId = reader.IsDBNull(reader.GetOrdinal("customer_id")) ? null : reader.GetInt32(reader.GetOrdinal("customer_id")),
                StaffId = reader.IsDBNull(reader.GetOrdinal("staff_id")) ? null : reader.GetByte(reader.GetOrdinal("staff_id")),
                Activo = reader.GetBoolean(reader.GetOrdinal("activo")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("fecha_creacion"))
            };
        }
    }
}
