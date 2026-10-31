using Microsoft.Data.SqlClient;
using SakilaRentalApp.Models.ViewModels;
using System.Data;

namespace SakilaRentalApp.Data
{
    public class ReporteRepository
    {
        private readonly DatabaseHelper _db;

        public ReporteRepository(DatabaseHelper db)
        {
            _db = db;
        }

        /// <summary>
        /// Reporte solicitado en el examen:
        /// Consulta ADO.NET que obtiene las 10 películas con mayor cantidad de alquileres utilizando
        /// INNER JOIN, COUNT, GROUP BY, ORDER BY y TOP sobre film ↓ inventory ↓ rental.
        /// </summary>
        public async Task<List<PeliculasMasAlquiladasViewModel>> GetPeliculasMasAlquiladasAsync(int top = 10)
        {
            var reporte = new List<PeliculasMasAlquiladasViewModel>();

            const string sql = @"
                SELECT TOP (@Top)
                    f.film_id,
                    f.title AS pelicula,
                    COUNT(r.rental_id) AS cantidad_alquileres,
                    f.rating,
                    f.release_year,
                    f.rental_rate,
                    (SELECT COUNT(*) FROM inventory i WHERE i.film_id = f.film_id) AS copias_inventario,
                    (SELECT COUNT(*) FROM inventory i WHERE i.film_id = f.film_id AND i.inventory_id NOT IN (
                        SELECT r2.inventory_id FROM rental r2 WHERE r2.return_date IS NULL
                    )) AS copias_disponibles
                FROM film f
                INNER JOIN inventory i ON f.film_id = i.film_id
                INNER JOIN rental r ON i.inventory_id = r.inventory_id
                GROUP BY f.film_id, f.title, f.rating, f.release_year, f.rental_rate
                ORDER BY cantidad_alquileres DESC;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top;

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                reporte.Add(new PeliculasMasAlquiladasViewModel
                {
                    FilmId = reader.GetInt32(reader.GetOrdinal("film_id")),
                    Titulo = reader.GetString(reader.GetOrdinal("pelicula")),
                    CantidadAlquileres = reader.GetInt32(reader.GetOrdinal("cantidad_alquileres")),
                    Rating = reader.IsDBNull(reader.GetOrdinal("rating")) ? "N/A" : reader.GetString(reader.GetOrdinal("rating")),
                    ReleaseYear = reader.IsDBNull(reader.GetOrdinal("release_year")) ? null : reader.GetString(reader.GetOrdinal("release_year")),
                    RentalRate = reader.GetDecimal(reader.GetOrdinal("rental_rate")),
                    CopiasInventario = reader.GetInt32(reader.GetOrdinal("copias_inventario")),
                    CopiasDisponibles = reader.GetInt32(reader.GetOrdinal("copias_disponibles"))
                });
            }

            return reporte;
        }

        /// <summary>
        /// Métricas generales para el dashboard de administración y bienvenida.
        /// </summary>
        public async Task<(int TotalPeliculas, int TotalAlquileres, int AlquileresActivos, int TotalClientes)> GetDashboardMetricsAsync()
        {
            const string sql = @"
                SELECT 
                    (SELECT COUNT(*) FROM film WHERE is_deleted = 0) AS TotalPeliculas,
                    (SELECT COUNT(*) FROM rental) AS TotalAlquileres,
                    (SELECT COUNT(*) FROM rental WHERE return_date IS NULL) AS AlquileresActivos,
                    (SELECT COUNT(*) FROM customer WHERE active = '1') AS TotalClientes;";

            using var conn = await _db.GetOpenConnectionAsync();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return (
                    reader.GetInt32(0),
                    reader.GetInt32(1),
                    reader.GetInt32(2),
                    reader.GetInt32(3)
                );
            }

            return (0, 0, 0, 0);
        }
    }
}
