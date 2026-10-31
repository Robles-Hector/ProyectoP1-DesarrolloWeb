using Microsoft.Data.SqlClient;
using SakilaRentalApp.Models;
using System.Data;

namespace SakilaRentalApp.Data
{
    public class RentalRepository
    {
        private readonly DatabaseHelper _db;

        public RentalRepository(DatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Regla de negocio 1: Disponibilidad.
        /// Verifica si existe una copia libre de la película y registra el alquiler en una transacción atómica.
        /// </summary>
        public async Task<(bool Success, string Message, int RentalId)> AlquilarPeliculaAsync(
            int filmId, int customerId, byte staffId = 1)
        {
            using var conn = await _db.GetOpenConnectionAsync();
            using var tx = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                // 1. Verificar si la película existe y no está eliminada lógicamente
                const string checkFilmSql = @"
                    SELECT is_deleted, title 
                    FROM film 
                    WHERE film_id = @FilmId;";

                string filmTitle = "";
                using (var checkFilmCmd = new SqlCommand(checkFilmSql, conn, tx))
                {
                    checkFilmCmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;
                    using var reader = await checkFilmCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        return (false, "La película especificada no existe.", 0);
                    }
                    bool isDeleted = reader.GetBoolean(0);
                    filmTitle = reader.GetString(1);

                    if (isDeleted)
                    {
                        return (false, "La película no se encuentra disponible para alquiler por haber sido retirada del catálogo.", 0);
                    }
                }

                // 2. Regla de negocio adicional: Verificar si el cliente ya tiene una copia en préstamo activo
                const string checkActiveRentalSql = @"
                    SELECT COUNT(*) 
                    FROM rental r 
                    INNER JOIN inventory i ON r.inventory_id = i.inventory_id 
                    WHERE i.film_id = @FilmId 
                      AND r.customer_id = @CustomerId 
                      AND r.return_date IS NULL;";

                using (var checkActiveCmd = new SqlCommand(checkActiveRentalSql, conn, tx))
                {
                    checkActiveCmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;
                    checkActiveCmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
                    int activeRentals = (int)(await checkActiveCmd.ExecuteScalarAsync() ?? 0);
                    if (activeRentals > 0)
                    {
                        return (false, $"Ya cuentas con una copia de '{filmTitle}' en préstamo activo. Por favor devuelve la copia actual antes de solicitar un nuevo alquiler del mismo título.", 0);
                    }
                }

                // 3. Buscar una copia de inventario disponible para esta película
                const string findCopySql = @"
                    SELECT TOP 1 i.inventory_id
                    FROM inventory i WITH (UPDLOCK, ROWLOCK)
                    WHERE i.film_id = @FilmId
                      AND i.inventory_id NOT IN (
                          SELECT r.inventory_id 
                          FROM rental r 
                          WHERE r.return_date IS NULL
                      )
                    ORDER BY i.inventory_id ASC;";

                int? availableInventoryId = null;
                using (var findCmd = new SqlCommand(findCopySql, conn, tx))
                {
                    findCmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;
                    var res = await findCmd.ExecuteScalarAsync();
                    if (res != null && res != DBNull.Value)
                    {
                        availableInventoryId = Convert.ToInt32(res);
                    }
                }

                // Si no hay copias libres, aplicar regla de disponibilidad
                if (!availableInventoryId.HasValue)
                {
                    const string checkStockSql = "SELECT COUNT(*) FROM inventory WHERE film_id = @FilmId;";
                    int totalStock = 0;
                    using (var stockCmd = new SqlCommand(checkStockSql, conn, tx))
                    {
                        stockCmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;
                        totalStock = (int)(await stockCmd.ExecuteScalarAsync() ?? 0);
                    }

                    if (totalStock == 0)
                    {
                        return (false, $"La película '{filmTitle}' no cuenta con copias registradas en inventario.", 0);
                    }

                    return (false, $"Lo sentimos, todas las copias de '{filmTitle}' se encuentran alquiladas actualmente. Intente más tarde.", 0);
                }

                // 4. Registrar el alquiler en la tabla rental
                const string insertRentalSql = @"
                    INSERT INTO rental (rental_date, inventory_id, customer_id, return_date, staff_id, last_update)
                    VALUES (GETDATE(), @InventoryId, @CustomerId, NULL, @StaffId, GETDATE());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int rentalId;
                using (var insertCmd = new SqlCommand(insertRentalSql, conn, tx))
                {
                    insertCmd.Parameters.Add("@InventoryId", SqlDbType.Int).Value = availableInventoryId.Value;
                    insertCmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
                    insertCmd.Parameters.Add("@StaffId", SqlDbType.TinyInt).Value = staffId;

                    rentalId = (int)(await insertCmd.ExecuteScalarAsync() ?? 0);
                }

                await tx.CommitAsync();
                return (true, $"¡Alquiler realizado con éxito! Ha alquilado '{filmTitle}' (Copia #{availableInventoryId.Value}).", rentalId);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al procesar el alquiler: {ex.Message}", 0);
            }
        }

        /// <summary>
        /// Reglas de negocio 2 y 3: Propiedad del alquiler y Estado del alquiler.
        /// Un cliente solo puede devolver sus propios alquileres y no puede devolver un alquiler ya devuelto.
        /// </summary>
        public async Task<(bool Success, string Message)> DevolverPeliculaAsync(
            int rentalId, int? customerId, bool esAdmin)
        {
            using var conn = await _db.GetOpenConnectionAsync();
            using var tx = conn.BeginTransaction(IsolationLevel.ReadCommitted);

            try
            {
                // 1. Consultar el estado y propiedad del alquiler
                const string selectSql = @"
                    SELECT r.rental_id, r.customer_id, r.return_date, f.title
                    FROM rental r
                    INNER JOIN inventory i ON r.inventory_id = i.inventory_id
                    INNER JOIN film f ON i.film_id = f.film_id
                    WHERE r.rental_id = @RentalId;";

                int rentalCustomerId;
                DateTime? returnDate;
                string filmTitle;

                using (var selectCmd = new SqlCommand(selectSql, conn, tx))
                {
                    selectCmd.Parameters.Add("@RentalId", SqlDbType.Int).Value = rentalId;
                    using var reader = await selectCmd.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        return (false, "El registro de alquiler no fue encontrado.");
                    }

                    rentalCustomerId = reader.GetInt32(reader.GetOrdinal("customer_id"));
                    returnDate = reader.IsDBNull(reader.GetOrdinal("return_date")) ? null : reader.GetDateTime(reader.GetOrdinal("return_date"));
                    filmTitle = reader.GetString(reader.GetOrdinal("title"));
                }

                // Regla de Negocio: Propiedad del Alquiler
                if (!esAdmin && (!customerId.HasValue || customerId.Value != rentalCustomerId))
                {
                    return (false, "Acceso denegado: Únicamente puede devolver los alquileres que le pertenecen a su cuenta.");
                }

                // Regla de Negocio: Estado del Alquiler (No devolver si ya fue devuelto)
                if (returnDate.HasValue)
                {
                    return (false, $"La película '{filmTitle}' ya fue devuelta previamente el {returnDate.Value:dd/MM/yyyy HH:mm}.");
                }

                // 2. Registrar la devolución
                const string updateSql = @"
                    UPDATE rental
                    SET return_date = GETDATE(),
                        last_update = GETDATE()
                    WHERE rental_id = @RentalId AND return_date IS NULL;";

                using (var updateCmd = new SqlCommand(updateSql, conn, tx))
                {
                    updateCmd.Parameters.Add("@RentalId", SqlDbType.Int).Value = rentalId;
                    int affected = await updateCmd.ExecuteNonQueryAsync();

                    if (affected == 0)
                    {
                        return (false, "No se pudo registrar la devolución porque la película ya figura como devuelta.");
                    }
                }

                await tx.CommitAsync();
                return (true, $"La película '{filmTitle}' ha sido devuelta satisfactoriamente.");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, $"Error al devolver la película: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene el historial de alquileres de un cliente específico (Mis Alquileres).
        /// </summary>
        public async Task<List<Rental>> GetAlquileresPorClienteAsync(int customerId)
        {
            var list = new List<Rental>();
            const string sql = @"
                SELECT 
                    r.rental_id,
                    r.rental_date,
                    r.inventory_id,
                    f.film_id,
                    f.title AS film_title,
                    f.rental_rate,
                    r.customer_id,
                    CONCAT(c.first_name, ' ', c.last_name) AS customer_name,
                    r.return_date,
                    r.staff_id,
                    CONCAT(s.first_name, ' ', s.last_name) AS staff_name,
                    r.last_update
                FROM rental r
                INNER JOIN inventory i ON r.inventory_id = i.inventory_id
                INNER JOIN film f ON i.film_id = f.film_id
                INNER JOIN customer c ON r.customer_id = c.customer_id
                INNER JOIN staff s ON r.staff_id = s.staff_id
                WHERE r.customer_id = @CustomerId
                ORDER BY r.rental_date DESC;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapRental(reader));
            }

            return list;
        }

        /// <summary>
        /// Obtiene el listado de alquileres para el panel de administración, con filtros de búsqueda y estado.
        /// </summary>
        public async Task<List<Rental>> GetAllAlquileresAdminAsync(
            string? buscarCliente = null, bool soloPendientes = false, int limit = 100)
        {
            var list = new List<Rental>();
            var whereClauses = new List<string>();

            if (soloPendientes)
            {
                whereClauses.Add("r.return_date IS NULL");
            }

            if (!string.IsNullOrWhiteSpace(buscarCliente))
            {
                whereClauses.Add("(c.first_name LIKE @Buscar OR c.last_name LIKE @Buscar OR f.title LIKE @Buscar)");
            }

            string whereSql = whereClauses.Count > 0 ? "WHERE " + string.Join(" AND ", whereClauses) : "";

            string sql = $@"
                SELECT TOP (@Limit)
                    r.rental_id,
                    r.rental_date,
                    r.inventory_id,
                    f.film_id,
                    f.title AS film_title,
                    f.rental_rate,
                    r.customer_id,
                    CONCAT(c.first_name, ' ', c.last_name) AS customer_name,
                    r.return_date,
                    r.staff_id,
                    CONCAT(s.first_name, ' ', s.last_name) AS staff_name,
                    r.last_update
                FROM rental r
                INNER JOIN inventory i ON r.inventory_id = i.inventory_id
                INNER JOIN film f ON i.film_id = f.film_id
                INNER JOIN customer c ON r.customer_id = c.customer_id
                INNER JOIN staff s ON r.staff_id = s.staff_id
                {whereSql}
                ORDER BY r.rental_date DESC;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Limit", SqlDbType.Int).Value = limit;

            if (!string.IsNullOrWhiteSpace(buscarCliente))
            {
                cmd.Parameters.Add("@Buscar", SqlDbType.VarChar).Value = $"%{buscarCliente.Trim()}%";
            }

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapRental(reader));
            }

            return list;
        }

        private static Rental MapRental(SqlDataReader reader)
        {
            return new Rental
            {
                RentalId = reader.GetInt32(reader.GetOrdinal("rental_id")),
                RentalDate = reader.GetDateTime(reader.GetOrdinal("rental_date")),
                InventoryId = reader.GetInt32(reader.GetOrdinal("inventory_id")),
                FilmId = reader.GetInt32(reader.GetOrdinal("film_id")),
                FilmTitle = reader.GetString(reader.GetOrdinal("film_title")),
                RentalRate = reader.GetDecimal(reader.GetOrdinal("rental_rate")),
                CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
                CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
                ReturnDate = reader.IsDBNull(reader.GetOrdinal("return_date")) ? null : reader.GetDateTime(reader.GetOrdinal("return_date")),
                StaffId = reader.GetByte(reader.GetOrdinal("staff_id")),
                StaffName = reader.IsDBNull(reader.GetOrdinal("staff_name")) ? null : reader.GetString(reader.GetOrdinal("staff_name")),
                LastUpdate = reader.GetDateTime(reader.GetOrdinal("last_update"))
            };
        }
    }
}
