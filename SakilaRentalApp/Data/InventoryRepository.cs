using Microsoft.Data.SqlClient;
using SakilaRentalApp.Models;
using System.Data;

namespace SakilaRentalApp.Data
{
    public class InventoryRepository
    {
        private readonly DatabaseHelper _db;

        public InventoryRepository(DatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Obtiene todas las copias en inventario de una película con el detalle de si están alquiladas y a quién.
        /// </summary>
        public async Task<List<InventoryItem>> GetCopiesByFilmIdAsync(int filmId)
        {
            var copias = new List<InventoryItem>();
            const string sql = @"
                SELECT 
                    i.inventory_id,
                    i.film_id,
                    f.title AS film_title,
                    i.store_id,
                    i.last_update,
                    CASE WHEN r.rental_id IS NOT NULL AND r.return_date IS NULL THEN 1 ELSE 0 END AS esta_alquilada,
                    r.rental_id AS current_rental_id,
                    CONCAT(c.first_name, ' ', c.last_name) AS current_customer_name,
                    r.rental_date AS current_rental_date
                FROM inventory i
                INNER JOIN film f ON i.film_id = f.film_id
                LEFT JOIN rental r ON i.inventory_id = r.inventory_id AND r.return_date IS NULL
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                WHERE i.film_id = @FilmId
                ORDER BY i.inventory_id ASC;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                copias.Add(new InventoryItem
                {
                    InventoryId = reader.GetInt32(reader.GetOrdinal("inventory_id")),
                    FilmId = reader.GetInt32(reader.GetOrdinal("film_id")),
                    FilmTitle = reader.GetString(reader.GetOrdinal("film_title")),
                    StoreId = reader.GetInt32(reader.GetOrdinal("store_id")),
                    LastUpdate = reader.GetDateTime(reader.GetOrdinal("last_update")),
                    EstaAlquilada = reader.GetInt32(reader.GetOrdinal("esta_alquilada")) == 1,
                    CurrentRentalId = reader.IsDBNull(reader.GetOrdinal("current_rental_id")) ? null : reader.GetInt32(reader.GetOrdinal("current_rental_id")),
                    CurrentCustomerName = reader.IsDBNull(reader.GetOrdinal("current_customer_name")) ? null : reader.GetString(reader.GetOrdinal("current_customer_name")),
                    CurrentRentalDate = reader.IsDBNull(reader.GetOrdinal("current_rental_date")) ? null : reader.GetDateTime(reader.GetOrdinal("current_rental_date"))
                });
            }

            return copias;
        }

        /// <summary>
        /// Agrega una nueva copia de inventario para una película específica.
        /// </summary>
        public async Task<int> AddCopyAsync(int filmId, int storeId = 1)
        {
            const string sql = @"
                INSERT INTO inventory (film_id, store_id, last_update)
                VALUES (@FilmId, @StoreId, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;
            cmd.Parameters.Add("@StoreId", SqlDbType.Int).Value = storeId;

            return (int)(await cmd.ExecuteScalarAsync() ?? 0);
        }

        /// <summary>
        /// Busca una copia de inventario disponible (no alquilada actualmente) para una película.
        /// </summary>
        public async Task<int?> GetAvailableInventoryIdAsync(int filmId, SqlConnection conn, SqlTransaction? tx = null)
        {
            const string sql = @"
                SELECT TOP 1 i.inventory_id
                FROM inventory i
                WHERE i.film_id = @FilmId
                  AND i.inventory_id NOT IN (
                      SELECT r.inventory_id 
                      FROM rental r 
                      WHERE r.return_date IS NULL
                  )
                ORDER BY i.inventory_id ASC;";

            using var cmd = tx != null ? new SqlCommand(sql, conn, tx) : new SqlCommand(sql, conn);
            cmd.Parameters.Add("@FilmId", SqlDbType.Int).Value = filmId;

            var result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value)
            {
                return Convert.ToInt32(result);
            }
            return null;
        }
    }
}
